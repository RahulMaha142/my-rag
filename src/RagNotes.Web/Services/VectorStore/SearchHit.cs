namespace RagNotes.Web.Services.VectorStore;

public sealed record SearchHit(
    float Score,
    int DocumentId,
    string FileName,
    int ChunkIndex,
    string Text);
