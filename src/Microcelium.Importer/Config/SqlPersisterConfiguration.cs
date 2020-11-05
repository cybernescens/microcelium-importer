using System;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Microcelium.Importer.Meta;
using Microcelium.Importer.Parsing;
using Microcelium.Importer.Persistence;
using Microsoft.Extensions.DependencyInjection;
using SqlDataReader= Microcelium.Importer.Parsing.SqlDataReader;

namespace Microcelium.Importer.Config
{
  /// <summary>
  ///  Configuration object that ultimated yields a <see cref="SqlBulkCopyPersister{TRecord}" />
  /// </summary>
  public class SqlPersisterConfiguration<TRecord> : PersisterConfiguration<TRecord>
    where TRecord : class, IImportRecord
  {
    /// <summary>
    ///   The type ultimated yielded form <see cref="PersisterConfiguration{TRecord}.Build" />
    /// </summary>
    public override Type PersisterType => typeof(SqlBulkCopyPersister<TRecord>);

    /// <summary>
    ///   The target database connection string
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    ///   The action to configure any <see cref="SqlBulkCopy" /> objects that are created
    /// </summary>
    public Action<SqlBulkCopy> BulkCopyConfigure { get; set; }

    /// <inheritdoc />
    public override void Build(FileImportConfiguration<TRecord> configuration)
    {
      var sc = configuration.ServiceCollection;
      sc.AddScoped<SqlConnection>(sp => new SqlConnection(ConnectionString));
      sc.AddScoped<SqlBulkCopy>(ConfigureBulkCopy);
      sc.AddScoped<
        IPersister<
          SqlParserReader<TRecord>, SqlDataReader>,
          SqlBulkCopyPersister<TRecord>>();
    }

    private SqlBulkCopy ConfigureBulkCopy(IServiceProvider serviceProvider)
    {
      var recordType = typeof(TRecord);
      var conn = serviceProvider.GetService<SqlConnection>();
      var bulkCopier = new SqlBulkCopy(conn);
      bulkCopier.BatchSize = 10 * 1000;
      bulkCopier.BulkCopyTimeout = 0;

      this.BulkCopyConfigure?.Invoke(bulkCopier);

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
      return bulkCopier;
    }
  }

  /// <summary>
  ///   Extensions to facilitate the configuration of a <see cref="SqlBulkCopyPersister{}" />
  /// </summary>
  public static class SqlPersisterConfigurationExtensions
  {
    /// <summary>
    ///  Configures a <see cref="SqlBulkCopyPersister{T}" />
    /// </summary>
    public static FileImportConfiguration<T> SqlPersister<T>(
      this FileImportConfiguration<T> importConfiguration,
      Action<SqlPersisterConfiguration<T>> config,
      SqlPersisterConfiguration<T> persister = null)
      where T : class, IImportRecord
    {
      persister = persister ?? new SqlPersisterConfiguration<T>();
      config?.Invoke(persister);
      importConfiguration.PersisterConfiguration = persister;
      return importConfiguration;
    }
  }
}
