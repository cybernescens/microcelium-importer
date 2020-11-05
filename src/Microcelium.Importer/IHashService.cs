using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Microcelium.Importer.Journal
{
  public interface IFileHashService
  {
    Task<FileHash> Compute(string file);
  }

  public abstract class FileHashService : IFileHashService
  {
    private const int BufferSize = 4096;

    /// <summary>
    /// Gets the concrete <see cref="HashAlgorithm"/>, i.e. <see cref="SHA1"/> or <see cref="MD5"/>
    /// </summary>
    /// <returns></returns>
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

  public class SHA384FileHashService : FileHashService
  {
    protected override HashAlgorithm GetHashAlgorithm() => SHA384Managed.Create();
  }

  /// <inheritdoc />
  [ObsoleteAttribute("Here for benchmarks, use SHA384FileHashService")]
  public class SHA1FileHashService : FileHashService
  {
    protected override HashAlgorithm GetHashAlgorithm() => SHA1.Create();
  }

  /// <inheritdoc />
  [ObsoleteAttribute("Here for benchmarks, use SHA384FileHashService")]
  public class MD5FileHashService : FileHashService
  {
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
