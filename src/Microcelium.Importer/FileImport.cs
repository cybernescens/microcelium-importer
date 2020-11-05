using System;
using Microcelium.Importer.Config;
using Microsoft.Extensions.DependencyInjection;

namespace Microcelium.Importer
{
  /// <summary>
  /// Starting Point for Configuration of File Imports
  /// </summary>
  public static class FileImport
  {
    /// <summary>
    /// Takes an existing <typeparamref name="TConfiguration" /> and applies the <paramref name="configure" /> action
    /// </summary>
    /// <typeparam name="TConfiguration">the configuration implementation of <see cref="FileImportConfiguration{TRecord}" /></typeparam>
    /// <typeparam name="TRecord">the type of record ultimately being imported</typeparam>
    /// <param name="cfg">the configuration object</param>
    /// <param name="configure">the action that performs configuration</param>
    public static TConfiguration Configure<TConfiguration, TRecord>(this TConfiguration cfg, Action<TConfiguration> configure)
      where TConfiguration : FileImportConfiguration<TRecord>
      where TRecord : class, IImportRecord
    {
      configure(cfg);
      return cfg;
    }

    /// <summary>
    /// Returns the an instantiated configuration object for the
    /// import service that uses File Helpers for the parsing
    /// and SQL Bulk copy for persisting
    /// </summary>
    /// <typeparam name="TRecord">the type of record ultimately being imported</typeparam>
    /// <param name="serviceCollection">the <see cref="IServiceCollection" /> for dependency injection configuration</param>
    public static FileHelperSqlDataReaderImportConfiguration<TRecord> FileHelperSqlImportService<TRecord>(IServiceCollection serviceCollection)
      where TRecord : class, IImportRecord
      => new FileHelperSqlDataReaderImportConfiguration<TRecord>(serviceCollection);
  }
}
