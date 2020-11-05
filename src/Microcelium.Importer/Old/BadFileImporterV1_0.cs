using System.Text;

namespace Microcelium.Importer
{
  public class BadFileImporterV1_0 : FileImporter
  {
    public BadFileImporterV1_0(string dsn)
      : base(typeof(BadFileImporterV1_0).FullName, 1.0m, dsn) { }

    public override bool FilenameMatches(string path) => true;

    public override bool CanImport(FileWrapper f, StringBuilder sbReason) => true;

    protected override void ImportInner(FileWrapper fw, StringBuilder sbImportDetail, out bool success, out IDbFileData dbFileData)
    {
      dbFileData = new NoOpDbFileData();
      success = true;
    }

    public override int DeleteFileData(int importEventId) => 0;

    public class NoOpDbFileData : IDbFileData
    {
      public void Save(int importEventId, string dsn) { }

      public void Delete(int importEventId, string dsn) { }

      public string Summary { get; }
    }
  }
}