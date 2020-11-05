using System;
using System.Diagnostics.CodeAnalysis;

namespace Microcelium.Importer.vNext.Impl
{
  /// <summary>
  ///   Reads excel files
  /// </summary>
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  public class ExcelFileReaderFactory : IFileReaderFactory
  {
    private readonly int columnMax;
    private readonly int rowMax;
    private readonly string[] columnAddressFilter;

    /// <summary>
    /// instantiates an <see cref="ExcelFileReaderFactory"/>
    /// </summary>
    /// <param name="columnMax">the maximum amount of columns to consider in the CSV conversion, 0 for unlimited</param>
    /// <param name="rowMax">the maximum amount of rows to consider in the CSV conversion, 0 for unlimited</param>
    public ExcelFileReaderFactory(int columnMax = 0, int rowMax = 0, params string[] columnAddressFilter)
    {
      this.columnMax = columnMax;
      this.rowMax = rowMax;
      this.columnAddressFilter = columnAddressFilter;
    }

    /// <summary>
    ///   Creates an <see cref="ExcelReader{T}" /> for ExcelFiles
    /// </summary>
    /// <typeparam name="T">type of record we're importing</typeparam>
    /// <param name="importEvent">the ImportEvent context</param>
    /// <param name="onHydrate">this is fired after read and after the entity has been hydrated</param>
    /// <param name="onRead">the is fired prior to read and before the entity has been hydrated and the line parsed</param>
    /// <returns></returns>
    public IFileReader CreateFileReader<T>(
      Repository.ImportEvent importEvent,
      Action<FileLineContext<T>> onHydrate = null,
      Action<FileLineContext<T>> onRead = null) where T : class, IImportRecord
      => new ExcelReader<T>(importEvent, onHydrate, onRead, null, columnMax, rowMax, columnAddressFilter);
  }
}