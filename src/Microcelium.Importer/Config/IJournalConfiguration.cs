using System;
using Microcelium.Importer.Journal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microcelium.Importer.Config
{
  interface IJournalConfiguration
  {
    void Build<TRecord>(FileImportConfiguration<TRecord> configuration)
       where TRecord : class, IImportRecord;
  }

  /// <summary>
  ///   The base class for configuring the import journal. The import journal
  ///  stores the history of what files have been imported.
  /// </summary>
  public abstract class JournalConfiguration : IJournalConfiguration
  {
    /// <summary>
    ///   The Connection String for the Journal
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    ///   Should we ensure all schema exists prior to import
    /// </summary>
    public bool EnsureSchema { get; set; }

    /// <summary>
    ///   This is the only has we're going to use
    /// </summary>
    public IFileHashService FileHashService => new SHA384FileHashService();

    /// <summary>
    ///   The <see cref="DbContextOptionsBuilder" /> action performed when
    ///   registering with the <see cref="IServiceCollection" />
    /// </summary>
    public abstract Action<IServiceProvider, DbContextOptionsBuilder> OptionsAction { get; }

    /// <inheritdoc />
    public abstract void Build<TRecord>(FileImportConfiguration<TRecord> configuration)
      where TRecord : class, IImportRecord;
  }
}
