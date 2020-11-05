using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FileHelpers;
using OfficeOpenXml;
using IDisposable = System.IDisposable;

namespace Microcelium.Importer.vNext.Impl
{
  /// <summary>
  /// An <see cref="IFileReader"/> for Excel files. THe <paramref name="T"/> should needs to be
  /// a DelimitedRecord where the delimiter is a Pipe: '|'
  /// </summary>
  /// <typeparam name="T">the record type</typeparam>
  [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
  public class ExcelReader<T> : ImportReader<T> where T : class, IImportRecord
  {
    private readonly int columnMax;
    private readonly int rowMax;
    private readonly HashSet<string> columnAddressFilter;
    private FileHelperEngine<T> engine;
    private const string Delimiter = "|";

    private static readonly Regex ColumnAddressRegex = new Regex(@"^(?<col>[A-Z]+):[A-Z]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AlphabeticCharacterRegex = new Regex(@"^[A-Z]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly bool checkAddressFilter;

    /// <inheritdoc />
    public ExcelReader(
      Repository.ImportEvent importEvent,
      Action<FileLineContext<T>> onHydrate = null,
      Action<FileLineContext<T>> onRead = null,
      IProgressReporter progress = null, 
      int columnMax = 0,
      int rowMax = 0,
      params string[] columnAddressFilter) : base(importEvent, onHydrate, onRead, progress)
    {
      this.columnMax = columnMax;
      this.rowMax = rowMax;
      this.columnAddressFilter = 
        new HashSet<string>(
        (columnAddressFilter ?? new string[0])
        .Select(
          x =>
            {
              var match = ColumnAddressRegex.Match(x);
              if (match.Success)
                return $"{match.Groups["col"].Value}";
              if (AlphabeticCharacterRegex.IsMatch(x))
                return x;
              throw new InvalidOperationException($"Unknown Column Address: {x}");
            })
        .ToArray());

      this.checkAddressFilter = this.columnAddressFilter.Any();
    }

    protected override EventEngineBase<T> CreateEngine()
    {
      engine = new FileHelperEngine<T>();
      return engine;
    }

    /// <inheritdoc />
    public override IDataReader ExecuteReader()
    {
      /* first convert to csv, get a temp file */
      using (var excel = new ExcelPackage(new FileInfo(ImportEvent.File)))
      using (var csv = ConvertToCsv(excel))
      using (var reader = new StreamReader(csv))
        return new FileDataReader<T>(
          new FileInMemoryEngine<T>(
            engine.ReadStreamAsList(reader, int.MaxValue)));
    }

    /// <inheritdoc />
    public override void Dispose()
    {
      (engine as IDisposable)?.Dispose();
    }

    private MemoryStream ConvertToCsv(ExcelPackage package)
    {
      package.DoAdjustDrawings = false;
      var worksheet = package.Workbook.Worksheets[1];

      var maximumColumnNumber = columnMax > 0 ? columnMax : worksheet.Dimension.End.Column;
      var currentRow = new List<string>(maximumColumnNumber);
      var maximumRowNumber = rowMax > 0 ? rowMax : worksheet.Dimension.End.Row;
      var currentRowNum = 1;

      var memory = new MemoryStream();

      using (var writer = new StreamWriter(memory, Encoding.UTF8, 4096, true))
      {
        while (currentRowNum <= maximumRowNumber)
        {
          BuildRow(worksheet, currentRow, currentRowNum, maximumColumnNumber);
          WriteRecordToFile(currentRow, writer, currentRowNum, maximumRowNumber);
          currentRow.Clear();
          currentRowNum++;
        }
      }

      memory.Seek(0, SeekOrigin.Begin);
      return memory;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="record">List of cell values</param>
    /// <param name="sw">Open Writer to file</param>
    /// <param name="rowNumber">Current row num</param>
    /// <param name="totalRowCount"></param>
    /// <remarks>Avoiding writing final empty line so bulk import processes can work.</remarks>
    private static void WriteRecordToFile(IList<string> record, StreamWriter sw, int rowNumber, int totalRowCount)
    {
      var commaDelimitedRecord = ToDelimitedString(record);
      var write = rowNumber == totalRowCount ? (Action<string>)sw.Write : sw.WriteLine;
      write(commaDelimitedRecord);
    }

    private void BuildRow(ExcelWorksheet worksheet, IList<string> currentRow, int currentRowNum, int maxColumnNumber)
    {
      string GetCellText(ExcelRangeBase cell)
        => cell?.Value == null ? string.Empty : cell.Value.ToString().Trim();

      void AddCellValue(string s) 
        => currentRow.Add(s.IndexOf(Delimiter[0]) >= 0 ? $"\"{s}\"" : s);
      
      for (var i = 1; i <= maxColumnNumber; i++)
      {
        /*
         var column = worksheet.Column(i);
         if (column.Hidden || worksheet.Column(i).Style.Hidden || worksheet.Cells[currentRowNum, i].Style.Hidden) 
          continue;*/
        var colAddress = ExcelCellBase.GetAddressCol(i);
        if (checkAddressFilter &&  !columnAddressFilter.Contains(colAddress, StringComparer.OrdinalIgnoreCase))
          continue;
        
        AddCellValue(GetCellText(worksheet.Cells[currentRowNum, i]));
      }
    }

    private static string ToDelimitedString(IList<string> list)
    {
      var result = new StringBuilder();
      for (var i = 0; i < list.Count; i++)
      {
        var initialStr = list[i];
        result.Append(initialStr);
        if (i < list.Count - 1)
          result.Append(Delimiter);
      }

      return result.ToString();
    }
  }
}