using System;

namespace Microcelium.Importer
{
  /// <summary>
  ///   contains both durative and transient data about the import
  ///   The durative part going into ImportEvent which can be read/written from
  ///   the db
  /// </summary>
  public class ImportResult
  {
    public readonly bool? DataExisted;

    /// <summary>
    ///   data was saved this time; it may or may not have existed previously
    /// </summary>
    public readonly bool DataSaved;

    /// <summary>
    ///   saveable object; data summary
    /// </summary>
    public readonly IDbFileData DbFileData;

    //todo: might add ExistingImportEvent

    /// <summary>
    ///   if there is an existing import event and we're not ignoring the db, it will be returned here; null if ignoring or it
    ///   doesn't exist
    /// </summary>
    public readonly ImportEvent ExistingImportEvent;

    /// <summary>
    ///   transient data
    ///   detail on import itself; if !Success, would list detailed debugging
    ///   info
    /// </summary>
    public readonly string ImportDetail;

    public readonly TimeSpan ImportDuration;

    /// <summary>
    ///   replacement for things like RiskFile; if this isn't imported, it can be null
    /// </summary>
    public readonly ImportEvent ImportEvent;

    /// <summary>
    ///   short textual summary of import itself; if !Success, would list
    ///   readonly for failure
    ///   also, may be saved in ImportEvent, but ImportEvent will be null
    ///   unless success, to this transient data will be important in
    ///   failure cases
    /// </summary>
    public readonly string ImportSummary;

    public readonly bool Success;

    public ImportResult(
      ImportEvent existingImportEvent,
      ImportEvent importEvent,
      IDbFileData dbFileData,
      string importSummary,
      string importDetail,
      bool success,
      bool? dataExisted,
      bool dataSaved,
      TimeSpan importDuration)
    {
      ExistingImportEvent = existingImportEvent;
      ImportEvent = importEvent;
      DbFileData = dbFileData;
      ImportSummary = importSummary;
      ImportDetail = importDetail;
      Success = success;
      DataExisted = dataExisted;
      DataSaved = dataSaved;
      ImportDuration = importDuration;
    }
  }
}