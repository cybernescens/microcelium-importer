using System;
using Microcelium.Automation;
using Microcelium.Importer.vNext.Impl;
using Microcelium.Importer.vNext.Repository;

namespace Microcelium.Importer.vNext
{
  /// <summary>
  /// Helper methods for configuring file import tasks
  /// </summary>
  public static class ImporterTask
  {
    /// <summary>
    ///   Creates a vNext <see cref="ImportFilesTask{T}"/> with a <see cref="FileReaderFactory"/> and <see cref="SqlBulkCopyPersister"/>.
    ///   This is the typical setup. ImporterName becomes the namespace qualified class name <see cref="IImportRecord"/>
    ///   we are importing
    /// </summary>
    /// <typeparam name="T">the type of file record being imported</typeparam>
    /// <param name="sourceUnc">the source UNC of files</param>
    /// <param name="targetDsn">the target DSN for the data</param>
    /// <param name="deriveImportMomentFromFileName">should we derive import moment from the file name</param>
    /// <param name="fileAcceptPredicate">
    ///   a filter predicate where the input argument is the full file path of the file;
    ///   return true to include the file. When this is null all files are accepted
    /// </param>
    /// <returns>a configured <see cref="ImportFilesTask{T}"/></returns>
    public static MicroceliumAction For<T>(string sourceUnc, string targetDsn, bool deriveImportMomentFromFileName = false, Func<string, bool> fileAcceptPredicate = null) where T : class, IImportRecord
    {
      return new ImportFilesTask<T>(
        sourceUnc,
        new ImportEventClient(targetDsn, typeof(T).FullName, new SHA1FileHashService(), deriveImportMomentFromFileName),
        new FileReaderFactory(),
        new DelegatePersisterFactory(() => new SqlBulkCopyPersister(targetDsn, typeof(T))),
        fileAcceptPredicate: fileAcceptPredicate);
    }

    /// <summary>
    ///   Creates a vNext <see cref="ImportFilesTask{T}"/> with an <see cref="ExcelFileReaderFactory"/> and <see cref="SqlBulkCopyPersister"/>.
    ///   This is the typical setup. ImporterName becomes the namespace qualified class name <see cref="IImportRecord"/>
    ///   we are importing
    /// </summary>
    /// <typeparam name="T">the type of file record being imported</typeparam>
    /// <param name="sourceUnc">the source UNC of files</param>
    /// <param name="targetDsn">the target DSN for the data</param>
    /// <param name="deriveImportMomentFromFileName">should we derive import moment from the file name</param>
    /// <param name="fileAcceptPredicate">
    ///   a filter predicate where the input argument is the full file path of the file;
    ///   return true to include the file. When this is null all files are accepted
    /// </param>
    /// <param name="columnMax">the maximum amount of columns to consider, 0 for unlimited</param>
    /// <param name="rowMax">the maximum amount of rows to consider, 0 for unlimited</param>
    /// <returns>a configured <see cref="ImportFilesTask{T}"/></returns>
    public static MicroceliumAction ForExcel<T>(string sourceUnc, string targetDsn, bool deriveImportMomentFromFileName = false, Func<string, bool> fileAcceptPredicate = null, int columnMax = 0, int rowMax = 0) where T : class, IImportRecord
    {
      return new ImportFilesTask<T>(
        sourceUnc,
        new ImportEventClient(targetDsn, typeof(T).FullName, new SHA1FileHashService(), deriveImportMomentFromFileName),
        new ExcelFileReaderFactory(columnMax, rowMax),
        new DelegatePersisterFactory(() => new SqlBulkCopyPersister(targetDsn, typeof(T))),
        fileAcceptPredicate: fileAcceptPredicate);
    }
  }
}
