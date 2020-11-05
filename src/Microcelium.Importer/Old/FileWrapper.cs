using System;
using System.Collections.Generic;
using System.IO;
using Microcelium.Util;

namespace Microcelium.Importer
{
  /// <summary>
  ///   wrap a file to cache its string[] lines and its SHA1
  ///   Can save to db as CmsFile, but this class is unrelated to db and
  ///   doesn't know about it. They would be linked by SHA1
  /// </summary>
  public class FileWrapper
  {
    /// <summary>
    ///   cache
    /// </summary>
    private FileInfo fileInfo;

    private int readlines;

    /// <summary>
    ///   cache
    /// </summary>
    private string sha1;

    public FileWrapper(string path)
    {
      FullPath = path;
    }

    private FileInfo FileInfo => fileInfo ?? (fileInfo = new FileInfo(FullPath));

    /// <summary>
    ///   When having called GetLines will return the line count calculated from there.
    ///   This would generally be the same as <see cref="GetTotalLineCount" />, but if
    ///   you have already iterated the file in <see cref="GetLines" /> then this is a good option
    /// </summary>
    public virtual int ReadLineCount => readlines;

    public DateTime LastWriteTime => FileInfo.LastWriteTime;

    public DateTime CreationTime => FileInfo.CreationTime;

    public long ByteLength => FileInfo.Length;

    public string FileName => FileInfo.Name;

    public string FullPath { get; }

    /// <summary>
    ///   Cached access to this file's sha1
    /// </summary>
    public string Sha1 => sha1 ?? (sha1 = Sha1Helper.GetSha1HexString(FullPath));

    public override string ToString() => string.Format("{0}; {1:n0} bytes", FileName, ByteLength);

    /// <summary>
    ///   Enumerates the lines in a file via a stream; be cautious reading all lines into
    ///   memory as you could potentially get an OutOfMemory exception
    /// </summary>
    public IEnumerable<string> GetLines()
    {
      readlines = 0;
      using (var stream = new FileStream(FullPath, FileMode.Open, FileAccess.Read))
      using (var reader = new StreamReader(stream))
      {
        while (!reader.EndOfStream)
        {
          readlines++;
          yield return reader.ReadLine();
        }
      }
    }

    /// <summary>
    ///   Will iterate the entire file to get an accurate line count
    /// </summary>
    /// <returns></returns>
    public int GetTotalLineCount()
    {
      var totallines = 0;
      using (var stream = new FileStream(FullPath, FileMode.Open, FileAccess.Read))
      using (var reader = new StreamReader(stream))
      {
        while (!reader.EndOfStream)
        {
          totallines++;
          reader.ReadLine();
        }
      }

      return totallines;
    }
  }
}