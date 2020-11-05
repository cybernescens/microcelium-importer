namespace Microcelium.Importer.Persistence
{
  /// <summary>
  ///   The results of executing an <see cref="IPersister{T, U}" />
  /// </summary>
  public class PersistenceResult
  {
    /// <summary>
    ///   The total records persisted
    /// </summary>
    public int TotalRecords { get; internal set; }
  }
}
