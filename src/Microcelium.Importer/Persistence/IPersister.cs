using System;
using System.Threading.Tasks;
using Microcelium.Importer.Parsing;

namespace Microcelium.Importer.Persistence
{
  /// <summary>
  ///  IPersister is responsible for taking an <see cref="IParserReader{U}" />
  /// and storing the data returned from iterating the <see cref="IParserReader{U}" />
  /// </summary>
  /// <typeparam name="T">The parser reader implementation type required by this persiter</typeparam>
  /// <typeparam name="U">The parser reader result implementation type required by this persister</typeparam>
  interface IPersister<T, U> : IPersister, IDisposable where T : IParserReader<U>
  {
    /// <summary>
    ///   Returns the <see cref="Task{PersistenceResult}" /> after performing the underlying
    /// persistence mechanism for iterating the <see cref="IParserReader{U}" />
    /// </summary>
    /// <param name="context">the current executing context</param>
    /// <param name="parserReader">the <see cref="IParserReader{U}" /> associated with the file</param>
    Task<PersistenceResult> WriteToServer(ImportContext context, T parserReader);
  }

  /// <summary>
  ///  Decorative interface, perdominantly for decoration, for any Persister
  /// </summary>
  public interface IPersister {}
}
