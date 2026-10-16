namespace RagNotes.Web.Services.Embeddings;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(
        string text,
        string model,
        CancellationToken cancellationToken = default);
}
