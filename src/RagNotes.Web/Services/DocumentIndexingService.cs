using Microsoft.EntityFrameworkCore;
using RagNotes.Web.Data;
using RagNotes.Web.Models;
using RagNotes.Web.Services.Chunking;
using RagNotes.Web.Services.Embeddings;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Web.Services;

public class DocumentIndexingService
{
    private readonly ITextChunker _chunker;
    private readonly IEmbeddingService _embeddings;
    private readonly IEmbeddingModelCatalog _catalog;
    private readonly IVectorStore _vectorStore;
    private readonly RagNotesDbContext _db;

    public DocumentIndexingService(
        ITextChunker chunker,
        IEmbeddingService embeddings,
        IEmbeddingModelCatalog catalog,
        IVectorStore vectorStore,
        RagNotesDbContext db)
    {
        _chunker = chunker;
        _embeddings = embeddings;
        _catalog = catalog;
        _vectorStore = vectorStore;
        _db = db;
    }

    public async Task<int> IndexAsync(
        int documentId,
        string fileName,
        string content,
        ChunkOptions options,
        string embeddingModel,
        CancellationToken cancellationToken = default)
    {
        var model = _catalog.Resolve(embeddingModel);
        var vectorSize = _catalog.GetVectorSize(model);

        await _vectorStore.EnsureCollectionAsync(model, vectorSize, cancellationToken);

        var chunks = _chunker.Chunk(content, documentId, options);
        if (chunks.Count == 0)
        {
            await UpsertIndexRowAsync(documentId, model, 0, cancellationToken);
            return 0;
        }

        // Replace points for this model only so re-runs stay idempotent.
        await _vectorStore.DeleteByDocumentAsync(documentId: documentId, embeddingModel: model, cancellationToken: cancellationToken);

        var records = new List<VectorRecord>(chunks.Count);
        foreach (var chunk in chunks)
        {
            var embedding = await _embeddings.EmbedAsync(chunk.Text, model, cancellationToken);
            records.Add(new VectorRecord(
                Id: QdrantVectorStore.CreatePointId(documentId, chunk.ChunkIndex),
                Vector: embedding,
                DocumentId: documentId,
                FileName: fileName,
                ChunkIndex: chunk.ChunkIndex,
                Text: chunk.Text));
        }

        await _vectorStore.StoreAsync(model, vectorSize, records, cancellationToken);
        await UpsertIndexRowAsync(documentId, model, records.Count, cancellationToken);
        return records.Count;
    }

    public Task<int> IndexIntoModelAsync(
        Document document,
        string embeddingModel,
        CancellationToken cancellationToken = default) =>
        IndexAsync(
            document.Id,
            document.FileName,
            document.Content,
            document.ChunkOptions,
            embeddingModel,
            cancellationToken);

    public async Task<int> ReindexAsync(
        int documentId,
        string fileName,
        string content,
        ChunkOptions options,
        CancellationToken cancellationToken = default)
    {
        var models = await _db.DocumentEmbeddingIndexes
            .Where(index => index.DocumentId == documentId)
            .Select(index => index.EmbeddingModel)
            .ToListAsync(cancellationToken);

        if (models.Count == 0)
        {
            models.Add(_catalog.DefaultModel);
        }

        var total = 0;
        foreach (var model in models)
        {
            total += await IndexAsync(
                documentId,
                fileName,
                content,
                options,
                model,
                cancellationToken);
        }

        return total;
    }

    public async Task RemoveFromModelAsync(
        int documentId,
        string embeddingModel,
        CancellationToken cancellationToken = default)
    {
        var model = _catalog.Resolve(embeddingModel);
        await _vectorStore.DeleteByDocumentAsync(model, documentId, cancellationToken);

        var row = await _db.DocumentEmbeddingIndexes.FindAsync(
            [documentId, model],
            cancellationToken);
        if (row is not null)
        {
            _db.DocumentEmbeddingIndexes.Remove(row);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(int documentId, CancellationToken cancellationToken = default)
    {
        var models = await _db.DocumentEmbeddingIndexes
            .Where(index => index.DocumentId == documentId)
            .Select(index => index.EmbeddingModel)
            .ToListAsync(cancellationToken);

        if (models.Count == 0)
        {
            models.Add(_catalog.DefaultModel);
        }

        foreach (var model in models)
        {
            await _vectorStore.DeleteByDocumentAsync(model, documentId, cancellationToken);
        }
    }

    private async Task UpsertIndexRowAsync(
        int documentId,
        string embeddingModel,
        int chunkCount,
        CancellationToken cancellationToken)
    {
        var row = await _db.DocumentEmbeddingIndexes.FindAsync(
            [documentId, embeddingModel],
            cancellationToken);

        if (row is null)
        {
            _db.DocumentEmbeddingIndexes.Add(new DocumentEmbeddingIndex
            {
                DocumentId = documentId,
                EmbeddingModel = embeddingModel,
                ChunkCount = chunkCount,
                IndexedAt = DateTime.UtcNow
            });
        }
        else
        {
            row.ChunkCount = chunkCount;
            row.IndexedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
