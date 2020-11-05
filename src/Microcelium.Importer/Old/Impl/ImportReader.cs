using System;
using System.Data;
using FileHelpers;
using FileHelpers.Events;
using Microcelium.Logging;

namespace Microcelium.Importer.vNext.Impl
{
  /// <summary>
  ///   Manages the <see cref="IFileHelperAsyncEngine{T}" /> that parses log files
  ///   so we can efficiently parse and iterate over data
  /// </summary>
  public abstract class ImportReader<T> : IFileReader where T : class, IImportRecord
  {
    /// <summary>
    /// the associated import event
    /// </summary>
    protected readonly Repository.ImportEvent ImportEvent;

    /// <summary>
    /// the underlying FileHelpers Engine
    /// </summary>
    protected readonly EventEngineBase<T> Engine;

    private readonly Action<FileLineContext<T>> onHydrate;
    private readonly Action<FileLineContext<T>> onRead;

    private FileLineContext<T> fileReadContext;

    /// <summary>
    /// Creates the FileHelpersEngine to use
    /// </summary>
    /// <returns></returns>
    protected abstract EventEngineBase<T> CreateEngine();

    /// <summary>
    ///   Create an instance pointing it at a file to load, the internal
    ///   file parsing engine instantiated, but not initialized
    /// <param name="importEvent">the correlated <see cref="Importer.ImportEvent"/></param>
    /// <param name="onHydrate">this is fired after read and after the entity has been hydrated</param>
    /// <param name="onRead">the is fired prior to read and before the entity has been hydrated and the line parsed</param>
    /// <param name="progress">implementation to report progress; defaults to <see cref="ProgressReporter"/></param>
    /// </summary>
    public ImportReader(
      Repository.ImportEvent importEvent,
      Action<FileLineContext<T>> onHydrate = null,
      Action<FileLineContext<T>> onRead = null,
      IProgressReporter progress = null)
    {
      this.ImportEvent = importEvent;
      this.onHydrate = onHydrate;
      this.onRead = onRead;
      progress = progress ?? new ProgressReporter(importEvent.FileByteLength);

      Engine = CreateEngine();
      Engine.AfterReadRecord += AfterRead;
      Engine.Progress += (_, e) => progress.Report(e.CurrentBytes);
      Engine.BeforeReadRecord += BeforeRead;
    }

    private void AfterRead(EngineBase engineBase, AfterReadEventArgs<T> e)
    {
      e.Record.ImportEventId = ImportEvent.Id;
      fileReadContext.Update(e);
      onHydrate?.Invoke(fileReadContext);
      /* can also be set via INotifyRead interface is reason for OR */
      e.SkipThisRecord = e.SkipThisRecord || fileReadContext.SkipThisRecord;
    }

    private void BeforeRead(EngineBase engineBase, BeforeReadEventArgs<T> e)
    {
      e.Engine.ErrorMode = e.LineNumber >= engineBase.TotalRecords - 1 ? ErrorMode.IgnoreAndContinue : ErrorMode.ThrowException;
      fileReadContext = new FileLineContext<T>(ImportEvent, e.Record, e.LineNumber, e.RecordLine);
      onRead?.Invoke(fileReadContext);
      if (fileReadContext.RawRecordChanged)
        e.RecordLine = fileReadContext.RawRecord;
      /* can also be set via INotifyRead interface is reason for OR */
      e.SkipThisRecord = e.SkipThisRecord || fileReadContext.SkipThisRecord;
    }

    /// <inheritdoc />
    public abstract IDataReader ExecuteReader();

    /// <inheritdoc />
    public abstract void Dispose();
  }
}
