using RagNotes.Web.Services.Embeddings;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Web.Services;

public class SemanticSearchService
{
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _vectorStore;

    public SemanticSearchService(
        IEmbeddingService embeddings,
        IVectorStore vectorStore)
    {
        _embeddings = embeddings;
        _vectorStore = vectorStore;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddings.EmbedAsync(query, cancellationToken);
        return await _vectorStore.SearchAsync(embedding, topK, cancellationToken);
    }
}
