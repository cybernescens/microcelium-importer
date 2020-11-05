namespace Microcelium.Importer.Journal.Model
{
  /// <summary>
  /// A record indicating that a file has failed import
  /// </summary>
  public class EntryFailed
  {
    /// <summary>
    /// The import file entry
    /// </summary>
    public virtual Entry Entry { get; set; }

    /// <summary>
    /// If the error happened during parsing this is the approximate line of failure
    /// </summary>
    public int ReportedLineNumber { get; set; }

    /// <summary>
    /// The type of error or exception
    /// </summary>
    public string ErrorType { get; set; }

    /// <summary>
    /// The error message
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    ///   The action taken on error. This can be: Continue, Rethrow or Delegated
    /// </summary>
    public string ErrorAction { get; set; }
  }
}
