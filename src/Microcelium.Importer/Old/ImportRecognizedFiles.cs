using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microcelium.Automation;

namespace Microcelium.Importer
{
  public class ImportRecognizedFiles : MicroceliumTask
  {
    private readonly ImporterCatalog catalog;
    private readonly Func<string, bool> filePattern;
    private readonly string folderPath;
    private readonly bool overwrite;
    private readonly bool removeFileAfterImport;

    private readonly List<Func<string, bool>> skipPatterns;

    public ImportRecognizedFiles(string folderPath, bool overwrite, ImporterCatalog catalog, Func<string, bool> filePattern = null, bool removeFileAfterImport = false, List<Func<string, bool>> skipPatterns = null)
    {
      this.folderPath = folderPath;
      this.overwrite = overwrite;
      this.catalog = catalog;
      this.filePattern = filePattern ?? (f => true);
      this.removeFileAfterImport = removeFileAfterImport;
      this.skipPatterns = skipPatterns ?? new List<Func<string, bool>>();
      Recurse = true;
    }

    /// <summary>
    ///   search subfolders; default is true
    /// </summary>
    public bool Recurse { get; set; }

    public override string Description => "Import any registered/recognized files in folder " + folderPath;

    /// <summary>
    ///   skip any file where the file part of the path matches any of the given patterns
    /// </summary>
    public void AddSkipPattern(Func<string, bool> pattern) => skipPatterns.Add(pattern);

    private PathResultOpt ProcessPath(string f, MicroceliumActionResultBuilder result)
    {
      var shortName = Path.GetFileName(f);
      if (skipPatterns.Any(skip => skip(f)))
      {
        var msg = "Skipping " + f + " because it matched a Skip Pattern";
        result.AddDetailInfoLine(msg);
        return PathResultOpt.PatternSkip;
      }

      var fw = new FileWrapper(f);
      var sbReason = new StringBuilder();
      var i = catalog.GetUniqueImporter(fw, sbReason);
      if (i == null)
      {
        result.AddDetailWarnLine("No importer: " + f);
        return PathResultOpt.NoImporter;
      }

      var dupOpt = overwrite ? DuplicateOpt.Overwrite : DuplicateOpt.Skip;
      var rslt = i.Import(fw, dupOpt);
      var existed = rslt.DataExisted != null && rslt.DataExisted.Value; //this should exist since we're not "ignoring db"

      if (existed)
      {
        // probably redundant check, but definitely want to avoid new source of null reference error
        if (rslt.ExistingImportEvent != null)
        {
          var existingFname = rslt.ExistingImportEvent.FileOriginalName;
          var fnameToReport = shortName == existingFname ? "of the same name" : existingFname;

          result.AddDetailInfoLine(
            "Data in file {0} already existed in ImportEventId {1}, file {2}",
            shortName,
            rslt.ExistingImportEvent.ImportEventId,
            fnameToReport);
        }

        if (overwrite)
        {
          if (!rslt.Success)
          {
            result.AddDetailErrLine("Fail overwrite on " + f);
            result.AddDetailErrLine(rslt.ImportDetail);
            return PathResultOpt.ExistOverwriteFail;
          }

          result.AddDetailInfoLine("Success overwrite on " + f);
          return PathResultOpt.ExistOverwriteOk;
        }

        if (rslt.DataSaved)
        {
          throw new ApplicationException("Unexpected: We said not to overwrite, the data existed, and importer is reporting that it saved");
        }

        return PathResultOpt.ExistSkip;
      }

      if (removeFileAfterImport)
        File.Delete(f);

      if (!rslt.Success)
      {
        result.AddDetailErrLine("Import new fail: " + f);
        result.AddDetailErrLine(rslt.ImportDetail);
        return PathResultOpt.ImportNewFail;
      }

      result.AddDetailInfoLine("Import new ok: " + f);
      return PathResultOpt.ImportNewOk;
    }

    protected override bool RunInner(MicroceliumActionResultBuilder result)
    {
      var so = Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
      var files = Directory.EnumerateFiles(folderPath, "*", so);
      var results = new List<PathResultOpt>();
      var resultPartition = Enum.GetNames(typeof(PathResultOpt)).ToList();
      var sbSummary = new StringBuilder();

      foreach (var f in files.Where(f => filePattern(f)))
        results.Add(ProcessPath(f, result));

      foreach (var rtype in resultPartition)
      {
        var r = (PathResultOpt)Enum.Parse(typeof(PathResultOpt), rtype);
        sbSummary.AppendFormat(r + ": " + results.Count(f => f == r) + "; ");
      }

      result.AddSummary(sbSummary.ToString());
      return !results.Any(f => f == PathResultOpt.ExistOverwriteFail || f == PathResultOpt.ImportNewFail);
    }

    /// <summary>
    ///   partition on possible options
    /// </summary>
    private enum PathResultOpt
    {
      NoImporter,
      PatternSkip,
      ExistSkip,
      ExistOverwriteOk,
      ImportNewOk,
      ExistOverwriteFail,
      ImportNewFail
    }
  }
}