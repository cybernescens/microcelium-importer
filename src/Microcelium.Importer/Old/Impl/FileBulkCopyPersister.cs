using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microcelium.Importer.vNext.Impl
{
  /// <summary>
  ///   Default Implementation of <see cref="SqlBulkCopy" /> essentially
  ///   just wraps a <see cref="IFilePersister" /> object
  /// </summary>
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  public class SqlBulkCopyPersister : IFilePersister
  {
    private readonly SqlBulkCopy bulkCopier;

    private static readonly ILog Log = LogProvider.For<SqlBulkCopyPersister>();

    private static readonly BindingFlags npi = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly BindingFlags pnpi = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private SqlConnection connection;
    private SqlTransaction tx;

    /// <inheritdoc />
    public SqlBulkCopyPersister(string dsn, Type recordType)
    {
      connection = new SqlConnection(dsn);
      connection.Open();
      tx = connection.BeginTransaction();
      bulkCopier = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, tx);
      /* probably going to need to toy with batch sizes and what not */
      bulkCopier.BatchSize = 10 * 1000;
      bulkCopier.BulkCopyTimeout = 0;

      recordType
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(x => x.GetCustomAttribute<PersistenceAttribute>() != null)
        .Select(x => (SourceName: x.Name, TargetName: x.GetCustomAttribute<PersistenceAttribute>().Name ?? x.Name))
        .ToList()
        .ForEach(x => bulkCopier.ColumnMappings.Add(x.SourceName, x.TargetName));

      var dpa = recordType.GetCustomAttribute<PersistenceAttribute>()?.Name ?? recordType.Name;
      if (string.IsNullOrEmpty(dpa))
        throw new InvalidOperationException("Record Type Missing Class PersistenceAttribute indicating destination table");

      bulkCopier.DestinationTableName = dpa;
    }

    /// <inheritdoc />
    public async Task<int> WriteToServerAsync(IDataReader reader)
    {
      try
      {
        await bulkCopier.WriteToServerAsync(reader);
        tx.Commit();
        return reader.RecordsAffected;
      }
      catch (SqlException ex)
      {
        Log.Warn(ex, "Interpreting SQL Bulk Copy Exception");
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
