using System.Threading.Tasks;
using Microcelium.Importer.Parsing;
using Microcelium.Importer.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Microcelium.Importer
{
  /// <summary>
  ///
  /// </summary>
  public interface IFileImportService
  {
    /// <summary>
    ///
    /// </summary>
    Task<FileImportResult> Execute();
  }
  /// <summary>
  /// Service for Importing Files, use
  /// </summary>
  public interface IFileImportService<TParser, TPersister> : IFileImportService
    where TParser : IParser
    where TPersister : IPersister
  {
  }
}
