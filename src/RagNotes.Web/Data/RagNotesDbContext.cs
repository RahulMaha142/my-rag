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
}