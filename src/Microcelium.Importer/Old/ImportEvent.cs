using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Microcelium.Importer
{
  public class ImportEventCollection : Collection<ImportEvent>
  {
    public ImportEventCollection(DataTable dt)
    {
      foreach (DataRow dr in dt.Rows)
        Add(new ImportEvent(dr));
    }
  }

  /// <summary>
  ///   saved to the database (would replace classes/tables like RiskFile
  ///   and the short-lived CmsFile)
  ///   Only records actual imports (not failures)
  ///   Contains info about the file imported, the translator used, ...
  /// </summary>
  public class ImportEvent
  {
    public long FileByteLength;
    public DateTime FileCreateMoment;
    public DateTime FileLastWriteMoment;
    public int FileLineCount;
    public string FileOriginalName;
    public string ImporterName;
    public string ImporterVersion;
    public DateTime ImportMoment;

    /// <summary>
    ///   chooses to insert vs. update when you call SaveToDb
    /// </summary>
    private bool InDb;

    public string Sha1;

    //returned by FileImporter!

    private ImportEvent() { }

    /// <summary>
    ///   data summary, not import summary
    /// </summary>
    public ImportEvent(FileWrapper fw, FileImporter fi, string summary, string notes)
    {
      Sha1 = fw.Sha1;
      FileOriginalName = fw.FileName;
      FileCreateMoment = fw.CreationTime;
      FileLastWriteMoment = fw.LastWriteTime;
      FileByteLength = fw.ByteLength;
      FileLineCount = fw.ReadLineCount != default(int) ? fw.ReadLineCount : fw.GetTotalLineCount();
      ImportMoment = DateTime.Now;
      ImporterName = fi.ImporterName;
      ImporterVersion = fi.ImporterVersion.ToString();
      Summary = summary;
      Notes = notes;
    }

    public ImportEvent(DataRow dr)
    {
      ImportEventId = Convert.ToInt32(Convert.ToInt64("ImportEventId"));
      Sha1 = Convert.ToString("Sha1");
      FileOriginalName = Convert.ToString("FileOriginalName");
      FileCreateMoment = Convert.ToDateTime("FileCreateMoment");
      FileLastWriteMoment = Convert.ToDateTime("FileLastWriteMoment");
      FileByteLength = Convert.ToInt64("FileByteLength");
      FileLineCount = Convert.ToInt32("FileLineCount");
      ImportMoment = Convert.ToDateTime("ImportMoment");
      ImporterName = Convert.ToString("ImporterName");
      ImporterVersion = Convert.ToString("ImporterVersion");
      Summary = dr["Summary"] == DBNull.Value ? null : Convert.ToString(dr["Summary"]);
      Notes = dr["Notes"] == DBNull.Value ? null : Convert.ToString(dr["Notes"]);
      InDb = true;
    }

    public int ImportEventId { get; private set; }

    /// <summary>
    ///   data summary, not import summary
    /// </summary>
    public string Summary { get; set; }

    public string Notes { get; set; }

    public static ImportEvent LoadBySha1(string sha1, string dsn) => GetBy(sha1, null, dsn);

    public static ImportEvent LoadByImportEventId(int importEventId, string dsn) => GetBy(null, importEventId, dsn);

    private static ImportEvent GetBy(string sha1, int? id, string dsn)
    {
      using (var conn = new SqlConnection(dsn))
      using (var cmd = conn.CreateCommand())
      {
        conn.Open();
        cmd.Parameters.Add("@sha1", SqlDbType.VarChar, 40).Value = !string.IsNullOrEmpty(sha1) ? sha1 : (object)DBNull.Value;
        cmd.Parameters.Add("@id", SqlDbType.Int).Value = id ?? (object)DBNull.Value;
        cmd.CommandText = @"
          select top 1 ImportEventId,
            Sha1,FileOriginalName,FileCreateMoment,FileLastWriteMoment,FileByteLength,FileLineCount,
            ImportMoment,ImporterName,ImporterVersion,Summary,Notes 
          from [ImportEvent] 
          where (isnull(@sha1, '') <> '' and Sha1 = @sha1)
            or (isnull(@id, 0) <> 0 and ImportEventId = @id)
            or (1 = 0)
        ";

        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            return reader.MapToEntity(
              r =>
                new ImportEvent
                  {
                    ImportEventId = r.Get<int>("ImportEventId"),
                    Sha1 = r.Get("Sha1"),
                    FileOriginalName = r.Get("FileOriginalName"),
                    FileCreateMoment = r.Get<DateTime>("FileCreateMoment"),
                    FileLastWriteMoment = r.Get<DateTime>("FileLastWriteMoment"),
                    FileByteLength = r.Get<long>("FileByteLength"),
                    FileLineCount = r.Get<int>("FileLineCount"),
                    ImportMoment = r.Get<DateTime>("ImportMoment"),
                    ImporterName = r.Get("ImporterName"),
                    ImporterVersion = r.Get("ImporterVersion"),
                    Summary = r.Get("Summary"),
                    Notes = r.Get("Notes")
                  });
          }
        }
      }

      return null;
    }

    public static void EnsureImportEventExists(string dsn)
    {
      if (DataAccess.TableExists("ImportEvent", dsn))
        return;

      var sql = @"
        create table ImportEvent(
	        ImportEventId int identity(1,1) not null,
	        Sha1 char(40) not null,
	        FileOriginalName varchar(100) not null,
	        FileCreateMoment datetime not null,
	        FileLastWriteMoment datetime not null,
	        FileByteLength bigint not null,
	        FileLineCount int not null,
	        ImportMoment datetime not null,
	        ImporterName varchar(50) not null,
	        ImporterVersion varchar(15) not null,
	        Summary varchar(200) null,
	        Notes varchar(5000) null,
          constraint PK_File primary key clustered  ( ImportEventId ),
          constraint UNQ_Sha1 unique nonclustered ( Sha1 )
        ) 
      ";
      DataAccess.ExecuteNonQuery(sql, dsn);
    }

    // if it's already in the database, update; otherwise, insert
    // for now
    public void SaveToDb(string dsn)
    {
      if (InDb)
        throw new ApplicationException("Resaves & updates not allowed");
      DbInsert(dsn);
    }

    private void DbInsert(string dsn)
    {
      var sql = @"
        insert into ImportEvent ( 
          Sha1, 
          FileOriginalName, 
          FileCreateMoment, 
          FileLastWriteMoment, 
          FileByteLength, 
          FileLineCount, 
          ImportMoment, 
          ImporterName, 
          ImporterVersion, 
          Summary, 
          Notes 
        ) values ( 
          @Sha1,
          @FileOriginalName,
          @FileCreateMoment,
          @FileLastWriteMoment,
          @FileByteLength,
          @FileLineCount,
          @ImportMoment,
          @ImporterName,
          @ImporterVersion,
          @Summary,
          @Notes
        )
        set @ScopeIdentity1 = scope_identity()
      ";

      var cmd = new SqlCommand(sql);
      cmd.Parameters.AddWithValue("@Sha1", Sha1);
      cmd.Parameters.AddWithValue("@FileOriginalName", FileOriginalName);
      cmd.Parameters.AddWithValue("@FileCreateMoment", FileCreateMoment);
      cmd.Parameters.AddWithValue("@FileLastWriteMoment", FileLastWriteMoment);
      cmd.Parameters.AddWithValue("@FileByteLength", FileByteLength);
      cmd.Parameters.AddWithValue("@FileLineCount", FileLineCount);
      cmd.Parameters.AddWithValue("@ImportMoment", ImportMoment);
      cmd.Parameters.AddWithValue("@ImporterName", ImporterName);
      cmd.Parameters.AddWithValue("@ImporterVersion", ImporterVersion);
      cmd.Parameters.AddWithValue("@Summary", Summary == null ? DBNull.Value : (object)Summary);
      cmd.Parameters.AddWithValue("@Notes", Notes == null ? DBNull.Value : (object)Notes);

      var paramScopeId1 = new SqlParameter("@ScopeIdentity1", SqlDbType.Int);
      paramScopeId1.Direction = ParameterDirection.Output;
      cmd.Parameters.Add(paramScopeId1);

      DataAccess.ExecuteNonQuery(cmd, dsn);

      ImportEventId = (int)paramScopeId1.Value;
      Debug.Assert(ImportEventId > 0);

      InDb = true;
    }

    /// <summary>
    ///   Returns rows affected; should be 0 or 1
    /// </summary>
    public static int DeleteByImportEventId(int importEventId, string dsn)
    {
      var sql = @"delete from ImportEvent where ImportEventId = @ImportEventId";
      var cmd = new SqlCommand(sql);
      cmd.Parameters.AddWithValue("@ImportEventId", importEventId);
      //return DataAccess.ExecuteNonQuery(cmd, ConfigMicrocelium.DsnAtrioCmsFile);
      return DataAccess.ExecuteNonQuery(cmd, dsn);
    }

    public static bool ImportEventIdExists(int importEventId, string dsn)
    {
      var sql = @"select count(*) from ImportEvent where ImportEventId = @ImportEventId";
      var cmd = new SqlCommand(sql);
      cmd.Parameters.AddWithValue("@ImportEventId", importEventId);
      var count = (int) DataAccess.ExecuteScalar(cmd, dsn);
      Debug.Assert(count == 0 || count == 1);
      return count == 1;
    }
  }
}