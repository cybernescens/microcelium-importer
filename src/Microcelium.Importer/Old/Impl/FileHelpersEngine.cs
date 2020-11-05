using System.Collections;
using System.Collections.Generic;
using FileHelpers;

namespace Microcelium.Importer.vNext.Impl
{
  internal class AsyncFileHelpersEngine<T> : IEngine<T> where T : class, IImportRecord
  {
    private readonly IFileHelperAsyncEngine<T> innerEngine;

    public AsyncFileHelpersEngine(IFileHelperAsyncEngine<T> engine)
    {
      this.innerEngine = engine;
    }

    public void Close() => innerEngine?.Close();

    public IEnumerator<T> GetEnumerator() => innerEngine.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose() => innerEngine?.Dispose();
  }
}