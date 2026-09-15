namespace RagNotes.Web.Models
{
  public class TextChunk
  {
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
  }
}