using FileHelpers.Events;

namespace Microcelium.Importer
{
  /// <summary>
  ///   Context around individual lines in a file
  /// </summary>
  public sealed class FileLineContext<TRecord> where TRecord : class, IImportRecord
  {
    private string rawRecord;

    public FileLineContext(ImportContext context, TRecord record, int lineNumber, string rawRecord)
    {
      Context = context;
      LineNumber = lineNumber;
      Record = record;
      this.rawRecord = rawRecord;
    }

    public ImportContext Context { get; }
    public int LineNumber { get; }
    public bool SkipThisRecord { get; set; }
    public TRecord Record { get; private set; }
    public bool RawRecordChanged { get; private set; }

    public string RawRecord
    {
      get => rawRecord;
      set
      {
        if (rawRecord != value)
        {
          rawRecord = value;
          RawRecordChanged = true;
        }
      }
    }

    internal void Update(AfterReadEventArgs<TRecord> e)
    {
      Record = e.Record;
    }
  }
}
