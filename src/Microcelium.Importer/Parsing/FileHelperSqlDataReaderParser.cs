using System;
using System.Data;
using FileHelpers;
using Microcelium.Importer.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Microcelium.Importer.Parsing
{
  /// <summary>
  /// A File Parser that utilizes FileHelpers for File Processing that processes records
  ///  into a <see cref="SqlDataReader" /> which implementes <see cref="IDataReader" />
  ///  which is ultimated persisted by a <see cref="SqlBulkCopyPersister" /> that can
  ///  effeciently import <see cref="IDataReader" />
  /// </summary>
  public class FileHelperSqlDataReaderParser<TRecord> : IParser<SqlParserReader<TRecord>, SqlDataReader>
    where TRecord : class, IImportRecord
  {
    private readonly IServiceProvider serviceProvider;
    private IDisposable engineRead;
    private SqlParserReader<TRecord> parserReader;

    public FileHelperSqlDataReaderParser(IServiceProvider serviceProvider)
    {
      this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public SqlParserReader<TRecord> CreateParserReader()
    {
      this.parserReader = this.serviceProvider.GetService<SqlParserReader<TRecord>>();
      return parserReader;
    }

    /// <inheritdoc />
    public void Dispose()
    {
      (parserReader as IDisposable)?.Dispose();
    }
  }
}
