namespace RagNotes.Web.Services.VectorStore;

public interface IVectorStore
{
    Task EnsureCollectionAsync(
        string embeddingModel,
        ulong vectorSize,
        CancellationToken cancellationToken = default);

    Task EnsureAllCollectionsAsync(
        IEnumerable<(string EmbeddingModel, ulong VectorSize)> models,
        CancellationToken cancellationToken = default);

    Task StoreAsync(
        string embeddingModel,
        ulong vectorSize,
        IEnumerable<VectorRecord> records,
        CancellationToken cancellationToken = default);

    Task DeleteByDocumentAsync(
        string embeddingModel,
        int documentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchHit>> SearchAsync(
        string embeddingModel,
        float[] embedding,
        int topK = 5,
        DocumentChunkFilter? documentFilter = null,
        CancellationToken cancellationToken = default);

    string CollectionNameFor(string embeddingModel);
}
