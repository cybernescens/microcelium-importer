using System;
using System.Threading.Tasks;
using Microcelium.Importer.Meta;
using Microcelium.Importer.Parsing;
using Microcelium.Importer.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Microcelium.Importer.Services
{
  /// <summary>
  /// A file import process that utilizes both FileHelpers and Sql Bulk Copy
  /// </summary>
  public class FileHelperSqlImportService<TRecord> : FileImportService,
    IFileImportService<FileHelperSqlDataReaderParser<TRecord>, SqlBulkCopyPersister<TRecord>>
      where TRecord : class, IImportRecord
  {
    /// <summary>
    ///   Instantiates an instance of a <see cref="FileHelperSqlImportService{TRecord}" />
    /// </summary>
    public FileHelperSqlImportService(IServiceProvider serviceProvider) : base(serviceProvider) { }

    /// <inheritdoc />
    protected override async Task<ImportContext> HandleProcess(IServiceScope scope, ImportContext importContext)
    {
      using var persister = scope.ServiceProvider.GetService<IPersister<SqlParserReader<TRecord>, SqlDataReader>>();
      using var parser = scope.ServiceProvider.GetService<IParser<SqlParserReader<TRecord>, SqlDataReader>>();
      using var reader = parser.CreateParserReader();

      await Task.WhenAll(
        persister
          .WriteToServer(importContext, reader)
          .ContinueWith(x => { importContext.FileRecordCount = x.Result.TotalRecords; }));

      return importContext;
    }
  }
}
