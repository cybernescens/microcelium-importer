using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microcelium.Logging;

namespace Microcelium.Importer.vNext.Repository
{
  /// <summary>
  ///   Interface to ImportEvent tables in MicroceliumCumulative
  /// </summary>
  public class ImportEventClient : IImportJournal
  {
    private readonly string importerType;
    private readonly IFileHashService fileHashService;
    private readonly bool deriveImportMomentFromFileName;
    private readonly SqlConnection connection;

    private static readonly ILog Log = LogProvider.For<ImportEventClient>();
    private static readonly Regex timestampFilenameRegex = new Regex(@"^(?<date>\d{8})(?<time>\d{4,6})?_[\w\-. ]+$", RegexOptions.Compiled);

    /// <summary>
    /// instantiates a new ImportEventClient for a particular Importer Type
    /// </summary>
    /// <param name="dsn">the target persistence store</param>
    /// <param name="importerType">the type of importer, generally use the target Record Types full name</param>
    /// <param name="fileHashService">service that does hashing, always use <see cref="SHA1FileHashService"/></param>
    /// <param name="deriveImportMomentFromFileName">
    /// some files are timestamped and so specify true to override the import moment with the timestamp in the filename, this looks for:
    /// yyyyMMdd_filename or yyyyMMddHHmm_filename or yyyyMMddHHmmss_filename
    /// </param>
    public ImportEventClient(string dsn, string importerType, IFileHashService fileHashService, bool deriveImportMomentFromFileName = false)
    {
      this.importerType = importerType;
      this.fileHashService = fileHashService;
      this.deriveImportMomentFromFileName = deriveImportMomentFromFileName;
      connection = new SqlConnection(dsn);
    }

    private string Version => GetType().Assembly.GetName().Version.ToString();

    private async Task EnsureConnection()
    {
      if (connection.State != ConnectionState.Open)
      {
        Log.Warn(() => "Sql Connection not open, opening...");
        await connection.OpenAsync().ConfigureAwait(false);
      }
    }

    /// <inheritdoc />
    public async Task<ImportBatch> GetLastBatch()
    {
      await EnsureConnection();

      using (var cmd = connection.CreateCommand())
      {
        cmd.Parameters.Add("@importerName", SqlDbType.VarChar, 50).Value = importerType;
        cmd.CommandText = @"
          select ib.ImportBatchId, ib.CreatedMoment, TotalImportEvents = count(*)
          from dbo.ImportBatch ib
          cross apply (
            select top (1) ImportBatchId
            from dbo.ImportBatch
            where ImporterName = @importerName
            order by CreatedMoment desc
          ) lastBatch
          left join dbo.ImportBatchEvent ibe on ibe.ImportBatchId = ib.ImportBatchId
          where ib.ImportBatchId = lastBatch.ImportBatchId
          group by ib.ImportBatchId, ib.CreatedMoment
        ";

        using (var r = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
          while (await r.ReadAsync().ConfigureAwait(false))
            return new ImportBatch(r.Get<int>("ImportBatchId"), r.Get<DateTime>("CreatedMoment"), r.Get<int>("TotalImportEvents"));
      }

      return null;
    }

    /// <inheritdoc />
    public async Task<HashSet<string>> GetExistingImportHashes(bool reprocessUnknowns, bool reprocessFailures)
    {
      await EnsureConnection();

      using (var cmd = connection.CreateCommand())
      {
        cmd.Parameters.Add("@importerName", SqlDbType.VarChar, 50).Value = importerType;
        cmd.Parameters.Add("@excludeUnknowns", SqlDbType.Bit).Value = reprocessUnknowns;
        cmd.Parameters.Add("@excludeFailures", SqlDbType.Bit).Value = reprocessFailures;
        cmd.CommandText = @"
          select Sha1
          from ImportEvent
          where ImporterName = @importerName
            and not ( @excludeUnknowns = 1 and StatusKey = 'Unknown' )
            and not ( @excludeFailures = 1 and StatusKey = 'Error' )
          group by Sha1
        ";
        using (var reader = cmd.ExecuteReader())
          return new HashSet<string>(await reader.MapToEntitiesAsync(x => x.Get("Sha1")));
      }
    }

    /// <inheritdoc />
    public async Task<ImportBatch> NewBatch()
    {
      await EnsureConnection();

      using (var cmd = connection.CreateCommand())
      {
        var now = DateTime.Now;
        cmd.Parameters.Add("@now", SqlDbType.DateTime).Value = now;
        cmd.Parameters.Add("@importerName", SqlDbType.VarChar, 50).Value = importerType;
        cmd.Parameters.Add("@importBatchId", SqlDbType.Int).Direction = ParameterDirection.Output;
        cmd.CommandText = @"
          insert into dbo.ImportBatch (CreatedMoment, ImporterName)
            values (@now, @importerName)
          select @importBatchId = scope_identity()
        ";

        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        var id = Convert.ToInt32(cmd.Parameters["@importBatchId"].Value);
        return new ImportBatch(id, now);
      }
    }

    /// <inheritdoc />
    public async Task<ImportEvent> NewImportEvent(FileHash file, ImportBatch currentBatch, bool reprocessUnknowns)
    {
      await EnsureConnection();

      DateTime GetImportMoment()
      {
        if (!deriveImportMomentFromFileName)
          return DateTime.Now;

        var match = timestampFilenameRegex.Match(file.FileInfo.Name);
        if (!match.Success)
        {
          Log.Warn(() => $"deriveImportMomentFromFileName is true, but filename: `{file.FileInfo.Name}` does not contain timestamp. Using DateTime.Now");
          return DateTime.Now;
        }

        var date = match.Groups["date"].Value;
        var time = match.Groups["time"].Value;
        var datetime = $"{date}{time.PadLeft(6, '0')}";
        if (!DateTime.TryParseExact(datetime, "yyyyMMddHHmmss", CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var moment))
        {
          Log.Warn(() => $"deriveImportMomentFromFileName is true, but filename: `{file.FileInfo.Name}` is not a valid timestamp. Using DateTime.Now");
          return DateTime.Now;
        }

        return moment;
      }

      using (var cmd = connection.CreateCommand())
      {
        cmd.Parameters.Add("@sha1", SqlDbType.Char, 40).Value = file.Hash;
        cmd.Parameters.Add("@fileOriginalName", SqlDbType.VarChar, 100).Value = file.FileInfo.Name;
        cmd.Parameters.Add("@fileCreateMoment", SqlDbType.DateTime).Value = file.FileInfo.CreationTime;
        cmd.Parameters.Add("@fileLastWriteMoment", SqlDbType.DateTime).Value = file.FileInfo.LastWriteTime;
        cmd.Parameters.Add("@fileByteLength", SqlDbType.BigInt).Value = file.FileInfo.Length;
        cmd.Parameters.Add("@importMoment", SqlDbType.DateTime, 50).Value = GetImportMoment();
        cmd.Parameters.Add("@importerName", SqlDbType.VarChar, 255).Value = importerType;
        cmd.Parameters.Add("@importerVersion", SqlDbType.VarChar, 50).Value = Version;
        cmd.Parameters.Add("@importBatchId", SqlDbType.Int).Value = currentBatch.Id;
        cmd.Parameters.Add("@status", SqlDbType.VarChar, 10).Value = ImportStatus.Available.ToString();

        cmd.CommandText = @"
          declare @importEventId int;
          declare @importerNameLength int;

          /* we want to use a larger length for this ideally 255, but is mostly 50 and sometimes 100 */
          select top 1 @importerNameLength = CHARACTER_MAXIMUM_LENGTH
          from INFORMATION_SCHEMA.COLUMNS
          where TABLE_NAME = 'ImportEvent' and TABLE_SCHEMA = 'dbo' and COLUMN_NAME = 'ImporterName'

          select top (1) @importEventId = ImportEventId
          from dbo.ImportEvent (nolock)
          where Sha1 = @sha1

          if(isnull(@importEventId, 0) = 0)
           begin
            insert into dbo.ImportEvent (
              Sha1, FileOriginalName, FileCreateMoment, FileLastWriteMoment, FileLineCount, FileByteLength, ImportMoment, ImporterName, ImporterVersion, StatusKey)
            values (@sha1, @fileOriginalName, @fileCreateMoment, @fileLastWriteMoment, 0, @fileByteLength, @importMoment, substring(@importerName, 0, @importerNameLength), @importerVersion, @status)

            set @importEventId = scope_identity();

            insert into dbo.ImportBatchEvent (ImportBatchId, ImportEventId)
            select @importBatchId, @importEventId
            where not exists (
              select * from dbo.ImportBatchEvent (nolock) where ImportBatchId = @importBatchId and ImportEventId = @importEventId
            )
          end

          select top (1)
            ie.ImportEventId,
            ib.ImportBatchId,
            BatchCreatedMoment = ib.CreatedMoment,
            ie.Sha1,
            ie.FileOriginalName,
            ie.FileCreateMoment,
            ie.FileLastWriteMoment,
            ie.FileByteLength,
            ie.FileLineCount,
            ie.ImportMoment,
            ie.ImporterName,
            ie.ImporterVersion,
            StatusKey = case
              when isnull(StatusKey, '') = '' and ib.ImportBatchId = @importBatchId then 'Available'
              when isnull(StatusKey, '') = '' and FileLineCount > 0 then 'Complete'
              when isnull(StatusKey, '') = '' and FileLineCount < 1 then 'Error' --legacy files don't mess with them
              else isnull(StatusKey, 'Unknown')
            end
          from dbo.ImportEvent ie (nolock)
          cross apply (
            select top (1) ibe.ImportBatchId, ib.CreatedMoment
            from dbo.ImportBatchEvent ibe (nolock)
            inner join dbo.ImportBatch ib (nolock) on ib.ImportBatchId =ibe.ImportBatchId
            where ibe.ImportEventId = ie.ImportEventId
            order by ib.CreatedMoment desc
          ) ib
          where ie.Sha1 = @sha1
        ";

        using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
          while (reader.Read())
          {
            var batch = reader.MapToEntity(
              rb => new ImportBatch(
              rb.Get<int>("ImportBatchId"),
              rb.Get<DateTime>("BatchCreatedMoment")));

            ImportStatus GetStatus(string s) =>
              (ImportStatus)Enum.Parse(typeof(ImportStatus), s, true);

            var importEvent = reader.MapToEntity(
              r =>
                new ImportEvent(r.Get<int>("ImportEventId"), batch, GetStatus(r.Get("StatusKey")))
                {
                  File = file.File,
                  Sha1 = r.Get("Sha1"),
                  FileOriginalName = r.Get("FileOriginalName"),
                  FileCreateMoment = r.Get<DateTime>("FileCreateMoment"),
                  FileLastWriteMoment = r.Get<DateTime>("FileLastWriteMoment"),
                  FileByteLength = r.Get<long>("FileByteLength"),
                  FileLineCount = r.Get<int>("FileLineCount"),
                  ImportMoment = r.Get<DateTime>("ImportMoment"),
                  ImporterName = r.Get("ImporterName"),
                  ImporterVersion = r.Get("ImporterVersion"),
                });

            importEvent.EnsureStatus(currentBatch, reprocessUnknowns);
            return importEvent;
          }

        Log.Error($"Did not resolve an ImportEvent; most likely this file has already been imported, but by a different Importer (Hash: '{file.Hash}')");
        throw new InvalidOperationException($"Unable to create ImportEvent. File: {file}");
      }
    }

    /// <inheritdoc />
    public Task<ImportEvent> CloseEvent(ImportEvent importEvent) => CloseEvent(importEvent, ImportStatus.Complete);

    /// <inheritdoc />
    public Task<ImportEvent> FailEvent(ImportEvent importEvent, Exception exception) => CloseEvent(importEvent, ImportStatus.Error, exception);

    private async Task<ImportEvent> CloseEvent(ImportEvent importEvent, ImportStatus status, Exception exception = null)
    {
      await EnsureConnection();

      using (var cmd = connection.CreateCommand())
      {
        cmd.Parameters.Add("@importEventId", SqlDbType.Int).Value = importEvent.Id;
        cmd.Parameters.Add("@importBatchId", SqlDbType.Int).Value = importEvent.ImportBatch.Id;
        cmd.Parameters.Add("@fileLineCount", SqlDbType.Int).Value = importEvent.FileLineCount;
        cmd.Parameters.Add("@error", SqlDbType.VarChar).Value = exception?.Message ?? (object)DBNull.Value;
        cmd.Parameters.Add("@status", SqlDbType.VarChar, 10).Value = status.ToString();
        cmd.CommandText = @"
          update dbo.ImportEvent set
            StatusKey = @status,
            FileLineCount = @fileLineCount,
            Summary = left(case
              when @status = 'Complete' then concat('Success, Loaded ', @fileLineCount, ' records')
              when @status = 'Error' and len(@error) > 0 then concat('Error, Exception: ', @error)
              when @status = 'Error' then 'Error, Unknown - no Exception recorded'
              when isnull(Summary, '') = '' then 'Unknown, unable to rectify status and may require investigation'
              else Summary
            end, 200),
            Notes = case
              when isnull(Notes, '') = '' and @status = 'Error' and len(@error) > 0 then concat('Error, Exception: ', @error)
              else Notes
            end
          where ImportEventId = @importEventId
        ";

        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
      }

      return await NewImportEvent(new FileHash(importEvent.File, importEvent.Sha1), importEvent.ImportBatch, false);
    }

    /// <inheritdoc />
    public async Task<ImportEvent> PendEvent(ImportEvent importEvent)
    {
      await EnsureConnection();

      using (var cmd = connection.CreateCommand())
      {
        cmd.Parameters.Add("@importEventId", SqlDbType.Int).Value = importEvent.Id;
        cmd.Parameters.Add("@importBatchId", SqlDbType.Int).Value = importEvent.ImportBatch.Id;
        cmd.Parameters.Add("@status", SqlDbType.VarChar, 10).Value = ImportStatus.Pending.ToString();
        cmd.CommandText = @"
          update dbo.ImportEvent set
            StatusKey = @status
          where ImportEventId = @importEventId

          insert into dbo.ImportBatchEvent (ImportBatchId, ImportEventId)
            select top 1 @importBatchId, @importEventId
            where not exists (
              select top 1 *
              from dbo.ImportBatchEvent
              where ImportBatchId = @importBatchId and ImportEventId = @importEventId )
        ";

        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
      }

      importEvent.ImportStatus = ImportStatus.Pending;
      return importEvent;
    }

    /// <inheritdoc />
    public Task<FileHash> CalculateHash(string file) => fileHashService.Compute(file);

    /// <inheritdoc />
    public void Dispose() => connection?.Dispose();
  }
}
