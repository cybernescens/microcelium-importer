using System;

namespace Microcelium.Importer.vNext.Repository
{
  public class ImportEvent
  {
    internal ImportEvent(int importEventId, ImportBatch originalBatch, ImportStatus status)
    {
      Id = importEventId;
      ImportBatch = originalBatch;
      ImportStatus = status;
    }

    public int Id { get; }

    /// <summary>
    /// The Last Batch that processed this Import Event
    /// </summary>
    public ImportBatch ImportBatch { get; private set; }

    /// <summary>
    /// The Hash of the File
    /// </summary>
    public string Sha1 { get; internal set; }

    /// <summary>
    /// Just the name of the File
    /// </summary>
    public string FileOriginalName { get; internal set; }

    /// <summary>
    /// Full Name of the file (File Path)
    /// </summary>
    public string File { get; internal set; }
    public DateTime FileCreateMoment { get; internal set; }
    public DateTime FileLastWriteMoment { get; internal set; }
    public long FileByteLength { get; internal set; }
    public int FileLineCount { get; internal set; }
    public DateTime ImportMoment { get; internal set; }
    public string ImporterName { get; internal set; }
    public string ImporterVersion { get; internal set; }
    public ImportStatus ImportStatus { get; internal set; }
    
    public override string ToString() => $"{Id}:{FileOriginalName} - {ImportStatus} [{Sha1}]";

    public void EnsureStatus(ImportBatch currentBatch, bool reprocessUnknowns = false)
    {
      if (ImportStatus != ImportStatus.Unknown || !reprocessUnknowns)
        return;

      ImportStatus = ImportStatus.Available;
      ImportBatch = currentBatch;
    }
  }

  public enum ImportStatus
  {
    /// <summary>
    /// Not generally used
    /// </summary>
    Unknown,

    /// <summary>
    /// ImportEvent is available to be processed
    /// </summary>
    Available,

    /// <summary>
    /// ImportEvent was tried at some point, but did not succeed
    /// </summary>
    Error,

    /// <summary>
    /// ImportEvent has been processed but not closed as complete
    /// </summary>
    Pending,

    /// <summary>
    /// ImportEvent has been processed, persisted and closed as complete
    /// </summary>
    Complete,
  }
}