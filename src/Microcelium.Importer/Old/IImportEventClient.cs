using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microcelium.Importer.vNext.Repository;

namespace Microcelium.Importer.vNext
{
  /// <summary>
  ///   Interface to ImportEvent data
  /// </summary>
  public interface IImportJournal : IDisposable
  {
    /// <summary>
    ///   Gets the last batch that was imported
    /// </summary>
    Task<ImportBatch> GetLastBatch();

    /// <summary>
    ///   Returns the entire set of <see cref="Repository.ImportEvent" />s for this Importer
    /// </summary>
    Task<HashSet<string>> GetExistingImportHashes(bool reprocessUnknowns, bool reprocessFailures);

    /// <summary>
    ///   When processing a round of files we wrap those in an <see cref="ImportBatch" />.
    ///   An <see cref="ImportBatch" /> is essentially all the <see cref="Repository.ImportEvent" />s generated
    /// </summary>
    /// <returns>the newly created or existing <see cref="Repository.ImportEvent" /></returns>
    Task<ImportBatch> NewBatch();

    /// <summary>
    ///   Attempts to create a new <see cref="Repository.ImportEvent" /> for a file. If the file appears to
    ///   already have an associated <see cref="Repository.ImportEvent" /> it returns the existing event
    /// </summary>
    /// <param name="file">the full path to the file we're importing</param>
    /// <param name="currentBatch">the currently processing batch</param>
    /// <param name="reprocessUnknowns"></param>
    /// <returns>a new or existing <see cref="Repository.ImportEvent" /></returns>
    Task<Repository.ImportEvent> NewImportEvent(FileHash file, ImportBatch currentBatch, bool reprocessUnknowns);

    /// <summary>
    ///   Once a file has been processed we attempt to close the <see cref="Repository.ImportEvent" />
    ///   which means we're going to just update the amount of lines we imported
    ///   and set a summary message
    /// </summary>
    /// <param name="importEvent">the <see cref="Repository.ImportEvent" /> to close</param>
    /// <returns>the updated <see cref="Repository.ImportEvent" /></returns>
    Task<Repository.ImportEvent> CloseEvent(Repository.ImportEvent importEvent);

    /// <summary>
    ///   Calculates a string representation of the files SHA1 hash
    /// </summary>
    /// <param name="file">the prospective file</param>
    /// <returns>a string version of the SHA1 hash</returns>
    Task<FileHash> CalculateHash(string file);

    /// <summary>
    /// Fails and closes an <see cref="Repository.ImportEvent"/> when an error is encountered
    /// </summary>
    /// <param name="importEvent">the <see cref="Repository.ImportEvent"/></param>
    /// <param name="exception">the exception encountered</param>
    /// <returns>the failed <see cref="Repository.ImportEvent"/></returns>
    Task<Repository.ImportEvent> FailEvent(Repository.ImportEvent importEvent, Exception exception);

    /// <summary>
    /// Pends the current <see cref="Repository.ImportEvent"/> while its being processed
    /// </summary>
    /// <param name="importEvent">the <see cref="Repository.ImportEvent"/> being processed</param>
    /// <returns>the pended <see cref="Repository.ImportEvent"/></returns>
    Task<Repository.ImportEvent> PendEvent(Repository.ImportEvent importEvent);
  }
}
