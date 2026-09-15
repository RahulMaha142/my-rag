using RagNotes.Web.Services.Chunking;
using RagNotes.Web.Services.Embeddings;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Web.Services;

public class DocumentIndexingService
{
    private readonly ITextChunker _chunker;
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _vectorStore;

    public DocumentIndexingService(
        ITextChunker chunker,
        IEmbeddingService embeddings,
        IVectorStore vectorStore)
    {
        _chunker = chunker;
        _embeddings = embeddings;
        _vectorStore = vectorStore;
    }

    public async Task<int> IndexAsync(
        int documentId,
        string fileName,
        string content,
        CancellationToken cancellationToken = default)
    {
        var chunks = _chunker.Chunk(content, documentId);
        if (chunks.Count == 0)
        {
            return 0;
        }

        var records = new List<VectorRecord>(chunks.Count);
        foreach (var chunk in chunks)
        {
            var embedding = await _embeddings.EmbedAsync(chunk.Text, cancellationToken);
            records.Add(new VectorRecord(
                Id: QdrantVectorStore.CreatePointId(documentId, chunk.ChunkIndex),
                Vector: embedding,
                DocumentId: documentId,
                FileName: fileName,
                ChunkIndex: chunk.ChunkIndex,
                Text: chunk.Text));
        }

        await _vectorStore.StoreAsync(records, cancellationToken);
        return records.Count;
    }

    public Task DeleteAsync(int documentId, CancellationToken cancellationToken = default)
        => _vectorStore.DeleteByDocumentAsync(documentId, cancellationToken);
}
