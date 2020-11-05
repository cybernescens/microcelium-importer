using Microcelium.Importer.Journal.Model;
using Microsoft.EntityFrameworkCore;

namespace Microcelium.Importer.Journal
{
  internal class DataContext : DbContext
  {
    /// <summary>
    ///   Database Journal Batches
    /// </summary>
    public DbSet<Batch> Batches { get; set; }

    /// <summary>
    ///   Database Journal Entries
    /// </summary>
    public DbSet<Entry> Entries { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.HasDefaultSchema("ImportJournal");
      modelBuilder.Entity<EntryCompleted>().ToTable(nameof(EntryCompleted));
      modelBuilder.Entity<EntryFailed>().ToTable(nameof(EntryFailed));
    }
  }
}
