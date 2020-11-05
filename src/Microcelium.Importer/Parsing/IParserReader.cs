using System;
using Microcelium.Importer.Persistence;

namespace Microcelium.Importer.Parsing
{
  /// <summary>
  ///   Creates the object responsible for iterating a parsed file.
  /// </summary>
  /// <typeparam name="T">the underlying type returned by the IParserReader</typeparam>
  public interface IParserReader<T> : IDisposable
  {
    /// <summary>
    ///   Returns the underlying Reader object ultimated required by the <see cref="IPersister" />
    /// </summary>
    /// <param name="importContext">the context for the current import</param>
    T CreateReader(ImportContext importContext);
  }
}
