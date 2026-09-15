namespace RagNotes.Web.Services.VectorStore;

public interface IVectorStore
{
    Task EnsureCollectionAsync(CancellationToken cancellationToken = default);

    Task StoreAsync(
        IEnumerable<VectorRecord> records,
        CancellationToken cancellationToken = default);

    Task DeleteByDocumentAsync(
        int documentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchHit>> SearchAsync(
        float[] embedding,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
