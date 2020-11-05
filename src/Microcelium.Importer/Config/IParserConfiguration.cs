using System;
using Microcelium.Importer.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace Microcelium.Importer.Config
{
  interface IParserConfiguration<TRecord> where TRecord : class, IImportRecord
  {
    void Build(FileImportConfiguration<TRecord> configuration);
  }

  /// <summary>
  ///   Base class for all the configuration of all Parsers
  /// </summary>
  public abstract class ParserConfiguration<TRecord>
    : IParserConfiguration<TRecord> where TRecord : class, IImportRecord
  {
    /// <summary>
    ///   The concrete type of the configured and built <see cref="IParser{,}" /> implementation
    /// </summary>
    public abstract Type ParserType {get;}

    /// <summary>
    ///  Builds the configured <see cref="IParser{,}" /> implementation.
    /// </summary>
    public abstract void Build(FileImportConfiguration<TRecord> configuration);
  }
}
