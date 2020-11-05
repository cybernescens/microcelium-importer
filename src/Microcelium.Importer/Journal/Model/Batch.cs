using System.Collections.Generic;

namespace Microcelium.Importer.Journal.Model
{
  /// <summary>
  /// A Batch is a collection of files imported in the same process
  /// </summary>
  public class Batch
  {
    /// <summary>
    ///   The database identifier
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    ///   The collection of import entries associated with this import batch
    /// </summary>
    public virtual ICollection<Entry> Entries { get; private set; }
  }
}
