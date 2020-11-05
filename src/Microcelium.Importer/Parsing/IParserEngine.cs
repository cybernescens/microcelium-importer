using System;
using System.Collections;
using System.Collections.Generic;
using FileHelpers;

namespace Microcelium.Importer.Parsing
{
  /// <summary>
  ///   The wrapper interface around FileHelper engine
  /// </summary>
  internal interface IParserEngine : IEnumerable, IDisposable
  {
    /// <summary>
    ///   Closes the underlying file parsing engine
    /// </summary>
    void Close();
  }

  /// <summary>
  ///   Thin wrapper around FileHelper engine
  /// </summary>
  internal sealed class FileHelperParserEngine : IParserEngine
  {
    private readonly FileHelperAsyncEngine engine;

    /// <summary>
    /// Standard Constructor
    /// </summary>
    public FileHelperParserEngine(FileHelperAsyncEngine engine)
    {
      this.engine = engine;
    }

    /// <summary>
    ///   Closes the file parsing engine
    /// </summary>
    public void Close()
    {
      engine.Close();
    }

    /// <summary>
    ///   Disposes the file parsing engine
    /// </summary>
    public void Dispose()
    {
      ((IDisposable)engine).Dispose();
    }

    /// <summary>
    ///   Gets the file parsing engine enumerator.
    /// This should be an enumeration of parsed records
    /// </summary>
    public IEnumerator GetEnumerator() => ((IEnumerable<object>)engine).GetEnumerator();
  }
}
