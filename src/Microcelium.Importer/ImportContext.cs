namespace Microcelium.Importer
{
  /// <summary>
  ///   Represents the current executing file's import context
  /// </summary>
  public class ImportContext
  {
    /// <summary>
    ///   the underlying journal store identifier
    /// </summary>
    public int ImportId { get; internal set; }

    /// <summary>
    ///   the path the file being imported
    /// </summary>
    public string FilePath { get; internal set; }

    /// <summary>
    ///   the understood number of lines or records in the file
    /// </summary>
    public long FileRecordCount {get;set;}
  }
}
