using System;

namespace Microcelium.Importer.Journal.Model
{
  /// <summary>
  /// A record indicating that a file is successfully imported
  /// </summary>
  public class EntryCompleted
  {
    /// <summary>
    ///   The file entry
    /// </summary>
    public virtual Entry Entry { get; set; }

    /// <summary>
    ///   The number of lines in the file
    /// </summary>
    public int Lines { get; set; }

    /// <summary>
    ///   The name of the importer used
    /// </summary>
    public string ImporterName { get; set; }

    /// <summary>
    ///   The assembly version of the importer used
    /// </summary>
    public string ImporterVersion { get; set; }
  }
}
