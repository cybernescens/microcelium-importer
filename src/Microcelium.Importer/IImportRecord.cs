using FileHelpers;

namespace Microcelium.Importer
{
  /// <summary>
  /// Represents an individual record/row of an <see cref="ImportEvent"/>
  /// </summary>
  public interface IImportRecord
  {
    /// <summary>
    /// Remember to mark this as [FieldHidden] (<see cref="FieldHiddenAttribute"/>)
    /// and also [Persistence(nameof(ImportEventId))] (<see cref="PersistenceAttribute"/>)
    /// </summary>
    int ImportEventId { get; set; }
  }
}
