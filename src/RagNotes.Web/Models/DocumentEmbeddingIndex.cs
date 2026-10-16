namespace RagNotes.Web.Models;

public class DocumentEmbeddingIndex
{
    public int DocumentId { get; set; }
    public string EmbeddingModel { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;

    public Document Document { get; set; } = null!;
}
