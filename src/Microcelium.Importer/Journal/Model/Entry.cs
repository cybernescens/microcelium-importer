using System;
using System.ComponentModel.DataAnnotations;

namespace Microcelium.Importer.Journal.Model
{
  /// <summary>
  /// Represents an individual file import event
  /// </summary>
  public class Entry
  {
    /// <summary>
    ///   The database identifier
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    ///   The batch... a set of entries imported during a process
    /// </summary>
    public virtual Batch Batch { get; set; }

    /// <summary>
    ///   The SHA384 hash of the file
    /// </summary>
    [StringLength(96), Required]
    public string Sha384 { get; set; }

    /// <summary>
    ///   The name of the file (excluding path)
    /// </summary>
    [StringLength(255), Required]
    public string Name { get; set; }

    /// <summary>
    ///   The originating path the to the file, this could be a UNC or URL to S3 or Azure Data Lake for example
    /// </summary>
    [StringLength(4096), Required]
    public string Path { get; set; }

    /// <summary>
    ///   The created date attribute of the file
    /// </summary>
    [Required]
    public DateTime MomentCreated { get; set; }

    /// <summary>
    ///   The last modified date attribute of the file
    /// </summary>
    [Required]
    public DateTime MomentLastWrite { get; set; }

    /// <summary>
    ///   The total number of bytes in the file
    /// </summary>
    [Required]
    public long ByteSize { get; set; }
  }
}
