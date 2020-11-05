using System;

namespace Microcelium.Importer.vNext.Impl
{
  /// <inheritdoc />
  public class FileReaderFactory : IFileReaderFactory
  {
    /// <inheritdoc />
    public IFileReader CreateFileReader<T>(Repository.ImportEvent importEvent, Action<FileLineContext<T>> onHydrate = null, Action<FileLineContext<T>> onRead = null)
      where T : class, IImportRecord => new FileReader<T>(importEvent, onHydrate, onRead);
  }
}