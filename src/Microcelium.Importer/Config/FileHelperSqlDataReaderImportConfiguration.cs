using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microcelium.Importer.Config
{
  /// <summary>
  ///
  /// </summary>
  public class FileHelperSqlDataReaderImportConfiguration<T> : FileImportConfiguration<T> where T : class, IImportRecord
  {
    /// <summary>
    ///
    /// </summary>
    public FileHelperSqlDataReaderImportConfiguration(IServiceCollection serviceCollection) : base(serviceCollection) { }
  }
}
