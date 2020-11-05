using System;
using Microcelium.Importer.Journal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Microcelium.Importer.Config
{
  /// <summary>
  /// Configuration when the Import Journal is maintained in SQL Server
  /// </summary>
  public class SqlServerJournalConfiguration : JournalConfiguration
  {
    /// <inheritdoc />
    public override Action<IServiceProvider, DbContextOptionsBuilder> OptionsAction
      => (sp, opt) => {};

    /// <inheritdoc />
    public override void Build<TRecord>(FileImportConfiguration<TRecord> configuration)
    {
      var sc = configuration.ServiceCollection;
      sc.AddDbContext<DataContext>(opt => {}, ServiceLifetime.Scoped);
      throw new System.NotImplementedException();
    }
  }

  /// <summary>
  ///   Configuration Extensions for configuring the import journal with SQL Server
  /// </summary>
  public static class SqlServerJournalConfigurationExtensions
  {
    /// <summary>
    ///   Instantiates and makes SQL Server Import Journal configuration available
    /// </summary>
    public static FileImportConfiguration<TRecord> SqlJournal<TRecord>(
      this FileImportConfiguration<TRecord> importConfiguration,
      Action<SqlServerJournalConfiguration> config,
      SqlServerJournalConfiguration journal = null)
        where TRecord : class, IImportRecord
    {
      journal = journal ?? new SqlServerJournalConfiguration();
      config?.Invoke(journal);
      importConfiguration.JournalConfiguration = journal;
      return importConfiguration;
    }
  }
}
