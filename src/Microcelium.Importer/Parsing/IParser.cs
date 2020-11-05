using System;

namespace Microcelium.Importer.Parsing
{
  /// <summary>
  ///  Parsers are created and called by the <see cref="FileImportService" />
  ///    to create ParserReaders which ultimately iterate over the records in a file
  /// </summary>
  /// <typeparam name="TParserReader">...</typeparam>
  /// <typeparam name="TReader">...</typeparam>
  interface IParser<TParserReader, TReader> : IParser, IDisposable
    where TParserReader : IParserReader<TReader>
    //where TRecord : class, IImportRecord

  {
    /// <summary>
    ///   Creates the ParserReader which iterates over the records in a file
    /// </summary>

    TParserReader CreateParserReader();
  }

  /// <summary>
  ///
  /// </summary>
  public interface IParser { }
}
