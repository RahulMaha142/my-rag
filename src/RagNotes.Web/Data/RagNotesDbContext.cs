using Microsoft.EntityFrameworkCore;
using RagNotes.Web.Models;

namespace RagNotes.Web.Data;

public class RagNotesDbContext : DbContext
{
    public RagNotesDbContext(DbContextOptions<RagNotesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<DocumentEmbeddingIndex> DocumentEmbeddingIndexes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(document => document.MaxTokens)
                .HasDefaultValue(ChunkOptions.DefaultMaxTokens)
                .HasSentinel(-1);
            // 0 is a real overlap. Without a non-zero sentinel, EF skips it on insert and SQLite stores 20.
            entity.Property(document => document.OverlapPercent)
                .HasDefaultValue(ChunkOptions.DefaultOverlapPercent)
                .HasSentinel(-1);
            entity.Property(document => document.PreferSentences)
                .HasDefaultValue(ChunkOptions.DefaultPreferSentences);
        });

        modelBuilder.Entity<DocumentEmbeddingIndex>(entity =>
        {
            entity.HasKey(index => new { index.DocumentId, index.EmbeddingModel });
            entity.Property(index => index.EmbeddingModel)
                .HasMaxLength(256)
                .IsRequired();
            entity.HasOne(index => index.Document)
                .WithMany(document => document.EmbeddingIndexes)
                .HasForeignKey(index => index.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
