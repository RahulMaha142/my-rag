using RagNotes.Web.Services.Embeddings;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Web.Services;

public class SemanticSearchService
{
    private readonly IEmbeddingService _embeddings;
    private readonly IEmbeddingModelCatalog _catalog;
    private readonly IVectorStore _vectorStore;

    public SemanticSearchService(
        IEmbeddingService embeddings,
        IEmbeddingModelCatalog catalog,
        IVectorStore vectorStore)
    {
        _embeddings = embeddings;
        _catalog = catalog;
        _vectorStore = vectorStore;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        string query,
        string embeddingModel,
        int topK = 5,
        DocumentChunkFilter? documentFilter = null,
        CancellationToken cancellationToken = default)
    {
        var model = _catalog.Resolve(embeddingModel);
        var embedding = await _embeddings.EmbedAsync(query, model, cancellationToken);
        return await _vectorStore.SearchAsync(
            model,
            embedding,
            topK,
            documentFilter,
            cancellationToken);
    }
}
