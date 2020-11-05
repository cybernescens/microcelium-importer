using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microcelium.Importer
{
  /// <summary>
  ///   can be a subclass of FileTranslator eventually, and common stuff can move up a level
  /// </summary>
  public abstract class FileImporter
  {
    /// <summary>
    ///   2012 12 03; changed from private to protected; not sure why it was private
    ///   Now the delete method can use it
    /// </summary>
    protected readonly string Dsn;

    public readonly string ImporterName;
    public readonly decimal ImporterVersion;

    private DateTime? runStartMoment; //moved here to give access to status event handler

    private IDictionary<string, ImportEvent> sha1Cache = new Dictionary<string, ImportEvent>();

    /// <summary>
    /// </summary>
    /// <param name="dsn">
    ///   DSN can be null if DuplicateOpt IgnoreDbCompletely
    ///   will be used. DSN is used for ImportEvent table and is passed to
    ///   the data object in its Save, etc. methods
    ///   DSN is NOT for the data object's storage--although it is probably usually
    ///   the same location.
    /// </param>
    public FileImporter(string importerName, decimal importerVersion, string dsn)
    {
      ImporterName = importerName;
      ImporterVersion = importerVersion;
      Dsn = dsn;
      StatusChanged += OnStatusChanged; // move into ctor 
    }

    public override string ToString() => ImporterName + " v" + ImporterVersion.ToString("0.0");

    public event Action<string, TimeSpan> StatusChanged; // class level 

    private void OnStatusChanged(string msg, TimeSpan timeSinceTaskStart) { } // class level 

    /// <summary>
    ///   new 2012 06 17
    ///   Trace + raise event (in case there are listeners)
    ///   Could add elapsed time, fraction complete estimate, etc.
    /// </summary>
    protected void UpdateStatus(string msg)
    {
      var timeSinceTaskStart = new TimeSpan(0);
      if (runStartMoment.HasValue)
        timeSinceTaskStart = DateTime.Now.Subtract(runStartMoment.Value);
      //string traceLine = FormatHelper.FriendlyTimeSpan(timeSinceTaskStart).PadLeft(15) + " since start: " + msg;
      var traceLine = FormatHelper.FriendlyTimeSpan(timeSinceTaskStart).PadLeft(15) + ": " + msg;
      Trace.TraceInformation(traceLine);
      StatusChanged(msg, timeSinceTaskStart);
    }

    /// <summary>
    ///   Given a filename or filepath, returns true if the filename matches the
    ///   filename pattern expected for the data imported by this importer. Note
    ///   that there would be circumstances where a slower, but more robust check
    ///   would be helpful since filenames could be altered.
    ///   2016-10-26: Pretty sure this is dead code
    /// </summary>
    public abstract bool FilenameMatches(string path);

    /// <summary>
    ///   Declare whether this importer can import the given file, based on
    ///   internal properties of the file (not on the filename)
    /// </summary>
    public abstract bool CanImport(FileWrapper f, StringBuilder sbReason);

    /// <summary>
    ///   it's assumed at this point that you have good reason to believe it CAN
    ///   be imported by this importer; if not, it would be correct to interpret
    ///   as an exceptional event
    ///   This method catches all import exceptions
    /// </summary>
    /// <param name="fw"></param>
    /// <param name="duplicateOpt"></param>
    /// <param name="dsn">Use for ImportEvent table; only used if duplicateOpt != IgnoreDbCompletely</param>
    /// <returns></returns>
    public ImportResult Import(FileWrapper fw, DuplicateOpt duplicateOpt) => Import(fw, duplicateOpt, null);

    /// <summary>
    ///   it's assumed at this point that you have good reason to believe it CAN
    ///   be imported by this importer; if not, it would be correct to interpret
    ///   as an exceptional event
    ///   This method catches all import exceptions
    /// </summary>
    /// <param name="dsn">Use for ImportEvent table; only used if duplicateOpt != IgnoreDbCompletely</param>
    public ImportResult Import(FileWrapper fw, DuplicateOpt duplicateOpt, string note)
    {
      runStartMoment = DateTime.Now;
      var start = DateTime.Now;

      bool? dataExisted = null;
      var overwriteIfExists = duplicateOpt == DuplicateOpt.Overwrite;
      var ignoreDbCompletely = duplicateOpt == DuplicateOpt.IgnoreDbCompletely;
      var sbSummary = new StringBuilder();
      ImportEvent existingImportEvent = null;
      bool doImport;
      if (ignoreDbCompletely)
      {
        doImport = true;
      }
      else // we need to know if it's there
      {
        existingImportEvent = ImportEvent.LoadBySha1(fw.Sha1, Dsn);
        dataExisted = existingImportEvent != null;
        var willOverwrite = dataExisted.Value && overwriteIfExists;
        doImport = willOverwrite || !dataExisted.Value;
        sbSummary.AppendFormat("Data did {0}previously exist in db; ", dataExisted.Value ? "" : "NOT ");
      }

      var success = true;
      var dataSaved = false;
      var sbImportDetail = new StringBuilder();
      ImportEvent importEvent = null;
      IDbFileData dbFileData = null;

      if (doImport)
      {
        try
        {
          ImportInner(fw, sbImportDetail, out success, out dbFileData); //import to data object
          if (success && !ignoreDbCompletely) // actually record in db
          {
            importEvent = new ImportEvent(fw, this, dbFileData.Summary, note ?? "");
            var existingEventId = existingImportEvent?.ImportEventId;
            if (existingEventId.HasValue)
            {
              Trace.TraceInformation(
                "The contents of file {0} already exist in the db as ImportEventId {1}",
                fw.FileName,
                existingEventId.Value);
            }

            SaveOrReplace(importEvent, dbFileData, existingEventId);
            dataSaved = true;
          }
        }
        catch (Exception e)
        {
          Trace.Fail($"Failure in importer: {ImporterName}{Environment.NewLine}{e}");
          sbSummary.Append("Failure in importer: " + ImporterName);
          sbImportDetail.AppendLine("Failure in importer: " + ImporterName);
          sbImportDetail.AppendLine(e.ToString());
          sbImportDetail.AppendLine(e.Message);
          success = false;
        }
      }
      else //skip
      {
        sbSummary.AppendFormat(
          @"Skipping: File has been imported by {0} version {1:0.0} as id {2}",
          existingImportEvent.ImporterName,
          existingImportEvent.ImporterVersion,
          existingImportEvent.ImportEventId);
      }

      var duration = DateTime.Now.Subtract(start);

      return new ImportResult(
        existingImportEvent,
        importEvent,
        dbFileData,
        sbSummary.ToString(),
        sbImportDetail.ToString(),
        success,
        dataExisted,
        dataSaved,
        duration);
    }

    /// <summary>
    ///   removes old as needed
    /// </summary>
    private void SaveOrReplace(ImportEvent importEvent, IDbFileData dbFileData, int? existingEventId)
    {
      //todo: these two should be done as a transaction
      //conceptual rollback
      try
      {
        try
        {
          if (existingEventId.HasValue)
          {
            Trace.TraceInformation("Deleting record(s) with existing ImportEventId {0}", existingEventId.Value);
            dbFileData.Delete(existingEventId.Value, Dsn);
            Trace.TraceInformation("Done with record(s) delete.");
            Trace.TraceInformation("Deleting import event record from ImportEvent table");
            ImportEvent.DeleteByImportEventId(existingEventId.Value, Dsn);
            Trace.TraceInformation("Done with import event record delete.");
          }
        }
        catch (Exception e2)
        {
          //Do not throw ApplicationExceptions https://msdn.microsoft.com/en-us/library/system.applicationexception(v=vs.110).aspx#Remarks
          throw new Exception("Serious error; manually fix db state", e2);
        }

        importEvent.SaveToDb(Dsn);
        dbFileData.Save(importEvent.ImportEventId, Dsn);
      }
      catch (Exception)
      {
        if (importEvent.ImportEventId > 0)
        {
          dbFileData.Delete(importEvent.ImportEventId, Dsn);
          //may or may not actually delete something, depending where exception was thrown
          ImportEvent.DeleteByImportEventId(importEvent.ImportEventId, Dsn);
        }

        throw;
      }
    }

    protected abstract void ImportInner(
      FileWrapper fw,
      StringBuilder sbImportDetail,
      out bool success,
      out IDbFileData dbFileData);

    public DateTime GetD8(string yyyyMMdd)
    {
      //Do not throw ApplicationExceptions https://msdn.microsoft.com/en-us/library/system.applicationexception(v=vs.110).aspx#Remarks
      if (yyyyMMdd.Length != 8)
        throw new Exception("Expected 8 chars");
      var syyyy = yyyyMMdd.Substring(0, 4);
      var sMM = yyyyMMdd.Substring(4, 2);
      var sdd = yyyyMMdd.Substring(6, 2);
      return new DateTime(int.Parse(syyyy), int.Parse(sMM), int.Parse(sdd));
    }

    /// <summary>
    ///   split into parts given the widths provided. Throw exception if the line's exact width
    ///   isn't the sum of the given widths.
    /// </summary>
    protected static string[] ExactFixedSplit(string lineToSplit, params int[] widths)
    {
      var widthOk = widths.Sum() == lineToSplit.Length;
      if (!widthOk)
        //Do not throw ApplicationExceptions https://msdn.microsoft.com/en-us/library/system.applicationexception(v=vs.110).aspx#Remarks
        throw new Exception($"Widths sum to {widths.Sum()}; line width is {lineToSplit.Length}");

      var parts = new List<string>();
      var start = 0;
      foreach (var w in widths)
      {
        parts.Add(lineToSplit.Substring(start, w));
        start += w;
      }

      return parts.ToArray();
    }

    /// <summary>
    ///   Delete from appropriate tables; the implementation should always be
    ///   delegated from Importer to the data object the importer returns (when
    ///   it's importing) because the importer isn't supposed to know about
    ///   db at all
    ///   2012 06 16 note: this should need a dsn parameter if it should exist at all
    ///   it may not need to exist
    ///   2012 12 03; Dsn is now protected instead of private so it should be available
    ///   2016-10-26: Pretty sure this is dead code
    /// </summary>
    /// <returns>record count deleted</returns>
    public abstract int DeleteFileData(int importEventId);

    /// <summary>
    ///   given file paths, return those matching the expected pattern for this
    ///   importer
    ///   2016-10-26: Pretty sure this is dead code
    /// </summary>
    public IEnumerable<string> GetMatchingFiles(IEnumerable<string> allFiles) => allFiles.Where(FilenameMatches);
  }

  /// <summary>
  ///   used to determine what to do with files that have already been imported
  ///   (records exist in object tables (e.g. Adua, etc) and/or CmsFile)
  /// </summary>
  public enum DuplicateOpt
  {
    Skip,
    Overwrite,
    IgnoreDbCompletely
  }
}