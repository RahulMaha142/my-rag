namespace RagNotes.Web.Services.VectorStore;

public sealed record VectorRecord(
    Guid Id,
    float[] Vector,
    int DocumentId,
    string FileName,
    int ChunkIndex,
    string Text);
