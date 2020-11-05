using System;
using FileHelpers;
using FileHelpers.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Microcelium.Importer.Parsing
{
  /// <summary>
  /// An implementation of <see cref="IParserReader{SqlDataReader}" />
  ///  that uses FileHelpers to parse the file and ultimated returns
  ///  a result set compatible with <see cref="System.Data.IDataReader" />
  /// </summary>
  public class SqlParserReader<TRecord> : IParserReader<SqlDataReader> where TRecord : class, IImportRecord
  {
    private readonly IServiceProvider serviceProvider;
    private readonly FileHelperAsyncEngine<TRecord> engine;
    private readonly IProgressReporter progressReporter;
    private readonly IExecuteBeforeReadRecord<TRecord> beforeRead;
    private readonly IExecuteAfterReadRecord<TRecord> afterRead;
    private IDisposable engineReader;

    /// <summary>
    /// Instantiates a SqlDataReaderParser
    /// </summary>
    public SqlParserReader(
      IServiceProvider serviceProvider,
      FileHelperAsyncEngine<TRecord> engine,
      IProgressReporter progressReporter,
      IExecuteBeforeReadRecord<TRecord> beforeRead,
      IExecuteAfterReadRecord<TRecord> afterRead)
    {
      this.serviceProvider = serviceProvider;
      this.engine = engine;
      this.progressReporter = progressReporter;
      this.beforeRead = beforeRead;
      this.afterRead = afterRead;
    }

    /// <inheritdoc />
    public SqlDataReader CreateReader(ImportContext importContext)
    {
      this.engine.BeforeReadRecord += (_, e) => OnBeforeReadRecord(_, e, importContext);
      this.engine.AfterReadRecord += (_, e) => OnAfterReadRecord(_, e, importContext);
      this.engineReader = engine.BeginReadFile(importContext.FilePath);
      return this.serviceProvider.GetService<SqlDataReader>();
    }

    private void OnBeforeReadRecord(EngineBase engine, BeforeReadEventArgs<TRecord> e, ImportContext importContext)
    {
      this.beforeRead.Action.Invoke(new FileLineContext<TRecord>(importContext, e.Record, e.LineNumber, e.RecordLine));
    }

    private void OnAfterReadRecord(EngineBase engine, AfterReadEventArgs<TRecord> e, ImportContext importContext)
    {
      e.Record.ImportEventId = importContext.ImportId;
      this.afterRead.Action.Invoke(new FileLineContext<TRecord>(importContext, e.Record, e.LineNumber, e.RecordLine));
    }

    /// <inheritdoc />
    public void Dispose()
    {
      this.engineReader?.Dispose();
      this.engine.Close();
      (this.engine as IDisposable)?.Dispose();
    }
  }
}
