using System;
using Microsoft.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microcelium.Importer.Meta;
using Microcelium.Importer.Parsing;
using Microsoft.Extensions.Logging;
using SqlDataReader = Microcelium.Importer.Parsing.SqlDataReader;

namespace Microcelium.Importer.Persistence
{
  /// <summary>
  ///   Persists records using <see cref="SqlBulkCopy" />
  /// </summary>
  public class SqlBulkCopyPersister<TRecord> : IPersister<SqlParserReader<TRecord>, SqlDataReader>
    where TRecord : class, IImportRecord
  {
    private readonly SqlBulkCopy bulkCopier;

    private static readonly ILogger Log = LogProvider.For<SqlBulkCopyPersister<TRecord>>();

    private static readonly BindingFlags npi = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly BindingFlags pnpi = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private SqlConnection connection;
    private SqlTransaction tx;

    internal SqlBulkCopyPersister(SqlConnection connection, SqlBulkCopy bulkCopier)
    {
      this.bulkCopier = bulkCopier;
      this.connection = connection;
      this.tx = connection.BeginTransaction();
    }

    /// <inheritdoc />
    public async Task<PersistenceResult> WriteToServer(ImportContext context, SqlParserReader<TRecord> parserReader)
    {
      using var reader = parserReader.CreateReader(context);

      try
      {
        await bulkCopier.WriteToServerAsync(reader);
        await tx.CommitAsync();
        return new PersistenceResult { TotalRecords = reader.RecordsAffected };
      }
      catch (SqlException ex)
      {
        Log.LogWarning(ex, "Interpreting SQL Bulk Copy Exception");
        if (ex.Message.Contains("Received an invalid column length from the bcp client"))
        {
          var match = Regex.Match(ex.Message, @"\d+");
          var index = Convert.ToInt32(match.Value) - 1;
          var fi = typeof(SqlBulkCopy).GetField("_sortedColumnMappings", npi);
          var sortedColumns = fi.GetValue(bulkCopier);
          var items = sortedColumns.GetType().GetField("_items", npi).GetValue(sortedColumns) as object[];
          var itemdata = items[index].GetType().GetField("_metadata", npi);
          var metadata = itemdata.GetValue(items[index]);
          var column = metadata.GetType().GetField("column", pnpi).GetValue(metadata);
          var length = metadata.GetType().GetField("length", pnpi).GetValue(metadata);

          reader.Close();
          tx.Rollback();
          throw new Exception($"Column: {column} contains data with a length greater than: {length}");
        }

        reader.Close();
        tx.Rollback();
        throw;
      }
      finally
      {
        connection.Close();
      }
    }

    /// <inheritdoc />
    public void Dispose()
    {
      bulkCopier?.Close();
      (bulkCopier as IDisposable)?.Dispose();
      tx?.Dispose();
      connection?.Dispose();
    }
  }
}
