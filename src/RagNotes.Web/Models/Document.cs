namespace RagNotes.Web.Models
{
  public class Document
  {
    public int Id { get; set;}
    public string FileName { get; set;} = string.Empty;
    public string Content { get; set;} = string.Empty;
    public DateTime CreatedAt { get; set;} = DateTime.UtcNow;
    public int MaxTokens { get; set; } = ChunkOptions.DefaultMaxTokens;
    public int OverlapPercent { get; set; } = ChunkOptions.DefaultOverlapPercent;
    public bool PreferSentences { get; set; } = ChunkOptions.DefaultPreferSentences;

    public ChunkOptions ChunkOptions => new(MaxTokens, OverlapPercent, PreferSentences);
  }
}