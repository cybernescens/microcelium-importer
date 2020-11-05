using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Microcelium.Importer
{
  /// <summary>
  ///   would have all versions, and only some would be marked as active for new
  ///   imports (the others could remain around for other reasons)
  /// </summary>
  public class ImporterCatalog
  {
    private readonly List<FileImporter> importers = new List<FileImporter>();

    public void Register(FileImporter importer) => importers.Add(importer);

    /// <summary>
    ///   register all importers found in given assembly
    ///   (uses reflection)
    /// </summary>
    public void RegisterAll(Assembly a) => throw new NotImplementedException();

    /// <summary>
    ///   register all importers found in given class type
    ///   (uses reflection)
    /// </summary>
    public void RegisterAll(Type t) => throw new NotImplementedException();

    public void Register(IEnumerable<FileImporter> importersToReg)
    {
      foreach (var i in importersToReg)
        Register(i);
    }

    /// <summary>
    ///   returns null if none found; throws exception if multiple found
    /// </summary>
    public FileImporter GetUniqueImporter(FileWrapper fw, StringBuilder sbReason)
    {
      FileImporter match = null;
      var matchCount = 0;
      foreach (var i in importers)
      {
        if (i.CanImport(fw, sbReason))
        {
          matchCount++;
          match = i;
        }
      }

      if (matchCount > 1)
      {
        throw new ApplicationException("Multiple (" + matchCount + ") importers matched");
      }

      return match;
    }
  }
}