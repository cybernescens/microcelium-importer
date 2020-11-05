using System;

namespace Microcelium.Importer.Parsing
{
  public interface IExecuteBeforeReadRecord<TRecord> where TRecord : class, IImportRecord
  {
    Action<FileLineContext<TRecord>> Action { get; }
  }

  public interface IExecuteAfterReadRecord<TRecord> where TRecord : class, IImportRecord
  {
    Action<FileLineContext<TRecord>> Action { get; }
  }

  public abstract class ExecuteReadRecord<TRecord> where TRecord : class, IImportRecord
  {
    public ExecuteReadRecord(Action<FileLineContext<TRecord>> action)
    {
      Action = action;
    }

    public Action<FileLineContext<TRecord>> Action { get; }
  }

  public class ExecuteBeforeReadRecord<TRecord> : ExecuteReadRecord<TRecord>, IExecuteBeforeReadRecord<TRecord>
    where TRecord : class, IImportRecord
  {
    /// <summary>
    /// Standard Constructor
    /// </summary>
    public ExecuteBeforeReadRecord(Action<FileLineContext<TRecord>> action) : base(action) { }
  }

  public class ExecuteAfterReadRecord<TRecord> : ExecuteReadRecord<TRecord>, IExecuteAfterReadRecord<TRecord>
    where TRecord : class, IImportRecord
  {
    /// <summary>
    /// Standard Constructor
    /// </summary>
    public ExecuteAfterReadRecord(Action<FileLineContext<TRecord>> action) : base(action) { }
  }

  public class NoOpBeforeReadRecord<TRecord> : ExecuteBeforeReadRecord<TRecord>
  where TRecord : class, IImportRecord
  {
    public NoOpBeforeReadRecord() : base(ctx => { }) { }
  }

  public class NoOpAfterReadRecord<TRecord> : ExecuteAfterReadRecord<TRecord>
   where TRecord : class, IImportRecord
  {
    public NoOpAfterReadRecord() : base(ctx => { }) { }
  }
}
