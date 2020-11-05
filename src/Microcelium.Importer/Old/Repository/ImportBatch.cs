using System;
using Microcelium.Automation;

namespace Microcelium.Importer.vNext.Repository
{
  /// <summary>
  ///   Represents a "batch" of import events, which is essentially a set of ImportEvents
  ///   with the same parent <see cref="MicroceliumActionResultSet" />
  /// </summary>
  public class ImportBatch : IEquatable<ImportBatch>
  {
    internal ImportBatch(int id, DateTime createdMoment)
    {
      Id = id;
      CreatedMoment = createdMoment;
    }

    public ImportBatch(int id, DateTime createdMoment, int totalImportEvents)
    {
      Id = id;
      CreatedMoment = createdMoment;
      TotalImportEvents = totalImportEvents;
    }

    internal ImportBatch(int id)
    {
      Id = id;
    }

    /// <summary>
    ///   Database Id
    /// </summary>
    public int Id { get;  }

    /// <summary>
    ///   First Started Import Event Start Moment
    /// </summary>
    public DateTime CreatedMoment { get; }

    /// <summary>
    ///   Total number of Import Events (files) in the batch
    /// </summary>
    public int TotalImportEvents { get; internal set; }

    public override string ToString() => $"{nameof(Id)}: {Id}, {nameof(CreatedMoment)}: {CreatedMoment:yyyy-MM-dd HH:mm}";

    public bool Equals(ImportBatch other)
    {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      return obj.GetType() == GetType() && Equals((ImportBatch)obj);
    }

    public override int GetHashCode() => Id;

    public static bool operator ==(ImportBatch left, ImportBatch right) => Equals(left, right);

    public static bool operator !=(ImportBatch left, ImportBatch right) => !Equals(left, right);

    public static bool operator <(ImportBatch left, ImportBatch right) => left.CreatedMoment < right.CreatedMoment;

    public static bool operator >(ImportBatch left, ImportBatch right) => left.CreatedMoment > right.CreatedMoment;
  }
}