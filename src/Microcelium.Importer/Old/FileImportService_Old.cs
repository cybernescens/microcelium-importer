using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microcelium.Automation;
using Microcelium.Importer.vNext.Repository;
using Microcelium.Logging;

namespace Microcelium.Importer.vNext
{
  /// <summary>
  /// Persists all available IIS files into a cumulative data store
  /// </summary>
  public class FileImportServiceOld
  {
    private readonly string localUnc;
    private readonly IImportJournal importEventClient;
    private readonly bool reprocessUnknowns;
    private readonly bool reprocessFailures;
    private readonly bool recurse;
    private readonly Func<string, bool> fileAcceptPredicate;
    private readonly IList<Repository.ImportEvent> unavailables = new List<Repository.ImportEvent>();
    private readonly IList<Repository.ImportEvent> errors = new List<Repository.ImportEvent>();
    private readonly ConcurrentDictionary<int, Exception> exceptions = new ConcurrentDictionary<int, Exception>();

    private MicroceliumActionResultBuilder currentResult;
    private int transientFileTotal;

    private static readonly ILog Log = LogProvider.GetCurrentClassLogger();
    private readonly Action<FileLineContext<T>> onFileLineHydrate;

    public ImportFilesTask(
      string localUnc,
      IImportJournal importEventClient,
      IFileReaderFactory logReaderFactory,
      IFilePersisterFactory logPersisterFactory,
      Action<FileLineContext<T>> onFileLineHydrate = null,
      Func<string, bool> fileAcceptPredicate = null,
      bool reprocessUnknowns = false,
      bool reprocessFailures = false,
      bool recurse = false)
    {
      LogReaderFactory = logReaderFactory;
      LogPersisterFactory = logPersisterFactory;
      this.localUnc = localUnc;
      this.importEventClient = importEventClient;
      this.onFileLineHydrate = onFileLineHydrate;
      this.reprocessUnknowns = reprocessUnknowns;
      this.reprocessFailures = reprocessFailures;
      this.recurse = recurse;
      this.fileAcceptPredicate = fileAcceptPredicate ?? (x => true);
      Key = $"Importer.vNext.{typeof(T).FullName}";
    }

    internal IFileReaderFactory LogReaderFactory { get; set; }
    internal IFilePersisterFactory LogPersisterFactory { get; set; }

    /// <inheritdoc />
    protected override async Task<bool> RunInnerAsync(MicroceliumActionResultBuilder result)
    {
      currentResult = result;
      var persistedHashes = await importEventClient.GetExistingImportHashes(reprocessUnknowns, reprocessFailures);
      result.Detail($"Retrieved already imported files...");
      var currentBatch = await importEventClient.NewBatch();
      result.Detail($"Created batch `{currentBatch.Id}`");

      result.Detail($"Getting list of potential imports `{localUnc}`");
      var localFiles = await Task.WhenAll(
        Directory.EnumerateFiles(localUnc, "*.*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
          .Where(x => fileAcceptPredicate(x))
          .Select(x => importEventClient.CalculateHash(x)));

      result.Detail($"Finding transient files...");
      var availableFiles =
        localFiles
          .Distinct(new FileHashEqualityComparer())
          .GroupJoin(persistedHashes, x => x.Hash, y => y, (x, y) => new { Target = x, Ignore = y.Any() })
          .Where(x => !x.Ignore)
          .Select(x => x.Target)
          .ToList();

      this.transientFileTotal = availableFiles.Count;
      var index = 0;
      var completedEvents = new List<Repository.ImportEvent>();

      foreach (var file in availableFiles)
        completedEvents.Add(await ImportFile(file, currentBatch, index++));

      var total = completedEvents.Count;
      var good = completedEvents.Count(x => x.ImportStatus == ImportStatus.Complete);
      var bad = completedEvents.Count(x => x.ImportStatus == ImportStatus.Error);
      var unavailable = unavailables.Count;
      var pass = bad < 1;

      foreach (var e in exceptions.Values)
        Log.Error(e, $"Unique Error Encountered Importing {typeof(T).Name}");

      result.AddSummary(
        $"{(pass ? "SUCCESS" : "FAILURE")}: Transient Files now Persisted. "
        + $"Successfully processed '{good:n0}' out of '{total:n0}' files. "
        + $"Error processing '{bad:n0}' out of '{total:n0}' files. "
        + $"Unavailable for processing '{unavailable:n0}' out of '{total:n0}' files. ");

      if (!pass)
        result.AddSummary(
          "Please investigate any failed files in the logs. "
          + "If bad and unable to fix add an ignore record via the Importer.Cmd tool. "
          + "This can be installed via dotnet tools: "
          + "`dotnet tool -g install Microcelium.Import.Cmd --add-source https://teamcity.inteligenz-cloud.com "
          + "--tool-path D:\\MicroceliumBin`");

      return pass;
    }

    private async Task<Repository.ImportEvent> ImportFile(FileHash file, ImportBatch currentBatch, int index)
    {
      /* get or create the ImportEvent */
      var importEvent = await importEventClient.NewImportEvent(file, currentBatch, reprocessUnknowns);
      Log.Info($"Import Event: `{importEvent}`");
      if (importEvent.ImportStatus == ImportStatus.Unknown && !reprocessUnknowns)
      {
        /* should be extremely rare */
        currentResult.Warn(() => $"`{file}` is candidate for processing, but does not appear to be available. "
          + $"This occurs when a file has an ImportEvent but the ImportEvent did not see "
          + $"any imported records. Skipping for now, to rerun, specify `reprocessUnknowns = true` "
          + $"when instantiating the task.");
        unavailables.Add(importEvent);
        return importEvent;
      }

      if (importEvent.ImportStatus == ImportStatus.Unknown)
        importEvent.EnsureStatus(currentBatch);

      /* does not appear that the SqlBulkCopier really enjoys being called
          concurrently; which sort of makes sense, so now we want to await
          the call, if this still doesn't work we may need to wait for all
          the import events to be created then one at a time */

      Exception error = null;
      var fail = false;

      try
      {
        importEvent = await Process(importEvent);
      }
      catch (Exception e)
      {
        fail = true;
        error = e.Demystify();
        var hc = $"{error.GetType().FullName}-{error.Message}".GetHashCode();
        if (!exceptions.ContainsKey(hc))
          exceptions.TryAdd(hc, error);
        Log.Error(error, $"Error Importing File {importEvent}");
      }

      if (fail)
      {
        /* don't forget to remove it in close */
        currentResult.Warn(() =>
          $"`{file}` encountered an error while importing. All records have been rolled back "
          + $"and ImportEvent `Id: {importEvent.Id}` will be set to 'Error'. See previous messages "
          + $"for error details. Error Message: `{error?.Message}`");
        errors.Add(importEvent);
      }

      if (index % 9 == 0 || index + 1 == transientFileTotal)
      {
        var ptg = (1m * index + 1) / transientFileTotal;
        currentResult.Detail(() =>
          $"Processed file with index `{(index + 1)}` of `{transientFileTotal}` ({ptg:p2}) "
          + $"(not guaranteed to be processed in order).");
      }

      return await (fail
        ? importEventClient.FailEvent(importEvent, error)
        : importEventClient.CloseEvent(importEvent));
    }

    private async Task<Repository.ImportEvent> Process(Repository.ImportEvent importEvent)
    {
      using (var persister = LogPersisterFactory.CreateFilePersister())
      using (var cmd = LogReaderFactory.CreateFileReader(importEvent, onFileLineHydrate))
      using (var rdr = cmd.ExecuteReader())
        await Task.WhenAll(
          importEventClient.PendEvent(importEvent),
          persister.WriteToServerAsync(rdr).ContinueWith(x => importEvent.FileLineCount = x.Result));

      if (importEvent.FileLineCount != 0)
        Log.Info($"Successfully imported {importEvent.FileLineCount:n0} records");

      return importEvent;
    }
  }
}
