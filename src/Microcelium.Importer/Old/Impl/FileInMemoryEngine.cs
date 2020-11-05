using System.Collections;
using System.Collections.Generic;

namespace Microcelium.Importer.vNext.Impl
{
  /// <summary>
  /// Just like it sounds, the entire file is stored in memory as hydrated objects
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class FileInMemoryEngine<T> : IEngine<T> where T : class, IImportRecord
  {
    private IList<T> records;

    public FileInMemoryEngine(IList<T> records)
    {
      this.records = records;
    }

    public void Close() => records = null;

    public IEnumerator<T> GetEnumerator() => records.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)records).GetEnumerator();

    public void Dispose() => records = null;
  }
}