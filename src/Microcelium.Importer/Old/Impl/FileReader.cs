using System;
using System.Data;
using FileHelpers;

namespace Microcelium.Importer.vNext.Impl
{
  public class FileReader<T> : ImportReader<T> where T : class, IImportRecord
  {
    private IDisposable engineRead;
    private FileHelperAsyncEngine<T> asyncEngine;

    /// <inheritdoc />
    public FileReader(
      Repository.ImportEvent importEvent,
      Action<FileLineContext<T>> onHydrate = null,
      Action<FileLineContext<T>> onRead = null,
      IProgressReporter progress = null) : base(importEvent, onHydrate, onRead, progress) { }

    /// <inheritdoc />
    protected override EventEngineBase<T> CreateEngine()
    {
      this.asyncEngine = new FileHelperAsyncEngine<T>();
      return asyncEngine;
    }

    /// <inheritdoc />
    public override IDataReader ExecuteReader()
    {
      engineRead = asyncEngine.BeginReadFile(ImportEvent.File);
      return new FileDataReader<T>(new AsyncFileHelpersEngine<T>(asyncEngine));
    }

    public override void Dispose()
    {
      engineRead?.Dispose();
      asyncEngine.Close();
      (asyncEngine as IDisposable)?.Dispose();
    }
  }
}