using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Microcelium.Importer.vNext
{
  /// <summary>
  ///   Service that Manages file hashes
  /// </summary>
  public interface IFileHashService
  {
    /// <summary>
    ///   Computes the hash of the provided file at file path provided in <paramref name="file" />
    /// </summary>
    /// <param name="file">the path to the file to hash</param>
    Task<FileHash> Compute(string file);
  }

  /// <summary>
  ///   Generic Service for hashing files asynchronously
  /// </summary>
  public abstract class FileHashService : IFileHashService
  {
    private const int BufferSize = 4096;

    /// <summary>
    /// Gets the concrete <see cref="HashAlgorithm"/>, i.e. <see cref="SHA1"/> or <see cref="MD5"/>
    /// </summary>
    protected abstract HashAlgorithm GetHashAlgorithm();

    /// <inheritdoc />
    public Task<FileHash> Compute(string file)
      => ComputeHash(file).ContinueWith(cw =>
        new FileHash(file, BitConverter.ToString(cw.Result).Replace("-", string.Empty)));

    private async Task<byte[]> ComputeHash(string path)
    {
      using (var hash = GetHashAlgorithm())
      using (var stream = File.OpenRead(path))
      {
        var buffer = new byte[BufferSize];
        var streamLength = stream.Length;
        hash.Initialize();

        while (true)
        {
          var read = await stream.ReadAsync(buffer, 0, BufferSize).ConfigureAwait(false);
          if (stream.Position == streamLength)
          {
            hash.TransformFinalBlock(buffer, 0, read);
            break;
          }

          hash.TransformBlock(buffer, 0, read, default(byte[]), default(int));
        }

        return hash.Hash;
      }
    }
  }

  /// <summary>
  ///   Hashes a file using the <see cref="SHA384Managed" /> class
  /// </summary>
  public class SHA384FileHashService : FileHashService
  {
    /// <inheritdoc />
    protected override HashAlgorithm GetHashAlgorithm() => SHA384Managed.Create();
  }

  /// <inheritdoc />
  [Obsolete("For comparison and historical purposes, use SHA384FileHashService")]
  public class SHA1FileHashService : FileHashService
  {
    /// <inheritdoc />
    protected override HashAlgorithm GetHashAlgorithm() => SHA1.Create();
  }

  /// <inheritdoc />
  [Obsolete("For comparison and historical purposes, use SHA384FileHashService")]
  public class MD5FileHashService : FileHashService
  {
    /// <inheritdoc />
    protected override HashAlgorithm GetHashAlgorithm() => MD5.Create();
  }

  /// <summary>
  /// Small wrapper around File with <see cref="System.IO.FileInfo"/> and the provided Hash
  /// (currently SHA384)
  /// </summary>
  public class FileHash
  {
    public FileHash(string file, string hash)
    {
      FileInfo = new FileInfo(file);
      Hash = hash;
    }

    public FileInfo FileInfo { get; }
    public string File => FileInfo.FullName;
    public string Hash { get; }

    public override string ToString() => File;
  }

  /// <summary>
  /// Equality Comparer for <see cref="FileHash"/> objects
  /// </summary>
  public class FileHashEqualityComparer : EqualityComparer<FileHash>
  {
    /// <inheritdoc />
    public override bool Equals(FileHash x, FileHash y) => x?.Hash == y?.Hash;

    /// <inheritdoc />
    public override int GetHashCode(FileHash obj) => obj?.Hash.GetHashCode() ?? 0;
  }
}
