using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microcelium.Importer
{
  /// <summary>
  ///   Abstract service which orchestrates the importing of a batch of files
  /// </summary>
  public abstract class FileImportService : IFileImportService
  {
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// Standard Constructor
    /// </summary>
    internal FileImportService(IServiceProvider serviceProvider)
    {
      this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task<FileImportResult> Execute()
    {
      throw new NotImplementedException();
    }

    /// <summary>
    ///   Processes a given file, but first wraps it in an appropriate service provider scope.
    ///   Do not override unless you know what you are doing
    /// </summary>
    protected virtual Task<ImportContext> Process(ImportContext importContext){
      using (var scope = serviceProvider.CreateScope())
        return HandleProcess(scope, importContext);
    }

    /// <summary>
    ///   Abstract Import Processing, this is where the bulk of the file import processing logic will go.
    /// </summary>
    /// <param name="scope">the current executing service provider scope</param>
    /// <param name="importContext">the current executing context</param>
    protected abstract Task<ImportContext> HandleProcess(IServiceScope scope, ImportContext importContext);
  }
}
