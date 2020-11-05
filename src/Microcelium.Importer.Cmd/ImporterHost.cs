using System;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using PowerArgs;
using Serilog;

namespace Microcelium.Importer.Cmd
{
  [ArgExceptionBehavior]
  public class ImporterHost
  {
    private void TryConnect(string getDsn, string importTable)
    {
      try
      {
        using (var conn = new SqlConnection(getDsn))
        {
          conn.Open();

          using (var cmd = conn.CreateCommand())
          {
            cmd.CommandText = $"select top 0 * from {importTable}";
            cmd.ExecuteNonQuery();
          }
        }
      }
      catch (Exception e)
      {
        throw new ImporterHostException($"Unable to establish database connection: {e.Message}");
      }
    }
  }

  public class IgnoreBadFileArgs
  {
    [ArgRequired]
    [ArgShortcut("-f")]
    [ArgDescription("The Fully qualified path to the known bad file")]
    public string File { get; set; }

    [ArgRequired]
    [ArgShortcut("-ic")]
    [ArgDescription("The Database that contains the ImportEvent table to record to")]
    public string InitialCatalog { get; set; }

    [ArgShortcut("-ds")]
    [ArgDescription("The Server that contains the ImportEvent database to record to")]
    public string DataSource { get; set; }

    public string GetDsn() => new SqlConnectionStringBuilder
      {
        DataSource = DataSource ?? Environment.MachineName,
        InitialCatalog = InitialCatalog,
        IntegratedSecurity = true
      }.ConnectionString;
  }
}
