using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RagNotes.Web.Data;
using RagNotes.Web.Models;
using RagNotes.Web.Options;
using RagNotes.Web.Services;
using RagNotes.Web.Services.Chunking;
using RagNotes.Web.Services.Embeddings;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Tests;

public class DocumentIndexingServiceTests
{
    [Fact]
    public async Task Reindex_deletes_existing_points_before_storing_new_chunks()
    {
        await using var db = await CreateDbAsync();
        var document = new Document
        {
            FileName = "note.md",
            Content = "hello world"
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        db.DocumentEmbeddingIndexes.Add(new DocumentEmbeddingIndex
        {
            DocumentId = document.Id,
            EmbeddingModel = "test-embed",
            ChunkCount = 1,
            IndexedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var store = new RecordingVectorStore();
        var catalog = new StubCatalog("test-embed", vectorSize: 2);
        var service = new DocumentIndexingService(
            new FixedChunker(chunkCount: 2),
            new StubEmbeddingService(),
            catalog,
            store,
            db);

        var count = await service.ReindexAsync(
            documentId: document.Id,
            fileName: "note.md",
            content: "hello world",
            new ChunkOptions(MaxTokens: 64, OverlapPercent: 10, PreferSentences: false));

        Assert.Equal(2, count);
        Assert.Equal(new[] { "ensure", "delete", "store" }, store.Calls);
        Assert.Equal(document.Id, store.DeletedDocumentId);
        Assert.Equal("test-embed", store.LastEmbeddingModel);
        Assert.Equal(2, store.Stored.Count);
        Assert.All(store.Stored, record =>
        {
            Assert.Equal(document.Id, record.DocumentId);
            Assert.Equal("note.md", record.FileName);
        });

        var index = await db.DocumentEmbeddingIndexes.SingleAsync();
        Assert.Equal(2, index.ChunkCount);
        Assert.Equal("test-embed", index.EmbeddingModel);
    }

    [Fact]
    public async Task IndexIntoModel_adds_a_second_embedding_without_touching_other_models()
    {
        await using var db = await CreateDbAsync();
        var document = new Document
        {
            FileName = "a.md",
            Content = "text"
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        db.DocumentEmbeddingIndexes.Add(new DocumentEmbeddingIndex
        {
            DocumentId = document.Id,
            EmbeddingModel = "bge",
            ChunkCount = 1,
            IndexedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var store = new RecordingVectorStore();
        var catalog = new StubCatalog("qwen", vectorSize: 2, extra: ("bge", 2));
        var service = new DocumentIndexingService(
            new FixedChunker(chunkCount: 1),
            new StubEmbeddingService(),
            catalog,
            store,
            db);

        var count = await service.IndexIntoModelAsync(document, "qwen");

        Assert.Equal(1, count);
        Assert.Equal("qwen", store.LastEmbeddingModel);
        Assert.Equal(2, await db.DocumentEmbeddingIndexes.CountAsync());
        var indexes = await db.DocumentEmbeddingIndexes.ToListAsync();
        Assert.Contains(indexes, index => index.EmbeddingModel == "bge");
        Assert.Contains(indexes, index => index.EmbeddingModel == "qwen" && index.ChunkCount == 1);
    }

    private static async Task<RagNotesDbContext> CreateDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<RagNotesDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new RagNotesDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private sealed class FixedChunker : ITextChunker
    {
        private readonly int _chunkCount;

        public FixedChunker(int chunkCount) => _chunkCount = chunkCount;

        public IReadOnlyList<TextChunk> Chunk(string text, int documentId, ChunkOptions options)
            => Enumerable.Range(0, _chunkCount)
                .Select(index => new TextChunk
                {
                    DocumentId = documentId,
                    ChunkIndex = index,
                    Text = $"{text} #{index}"
                })
                .ToList();
    }

    private sealed class StubEmbeddingService : IEmbeddingService
    {
        public Task<float[]> EmbedAsync(
            string text,
            string model,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new[] { 0.1f, 0.2f });
    }

    private sealed class StubCatalog : IEmbeddingModelCatalog
    {
        private readonly Dictionary<string, ulong> _sizes;

        public StubCatalog(string defaultModel, ulong vectorSize, params (string Id, ulong Size)[] extra)
        {
            DefaultModel = defaultModel;
            _sizes = new Dictionary<string, ulong>(StringComparer.Ordinal)
            {
                [defaultModel] = vectorSize
            };
            foreach (var (id, size) in extra)
                _sizes[id] = size;

            AvailableModels = _sizes
                .Select(pair => new EmbeddingModelOption { Id = pair.Key, VectorSize = pair.Value })
                .ToList();
        }

        public string DefaultModel { get; }

        public IReadOnlyList<EmbeddingModelOption> AvailableModels { get; }

        public string Resolve(string? requested) =>
            string.IsNullOrWhiteSpace(requested) ? DefaultModel : requested.Trim();

        public ulong GetVectorSize(string modelId) => _sizes[modelId];
    }

    private sealed class RecordingVectorStore : IVectorStore
    {
        public List<string> Calls { get; } = [];
        public int? DeletedDocumentId { get; private set; }
        public string? LastEmbeddingModel { get; private set; }
        public List<VectorRecord> Stored { get; } = [];

        public Task EnsureCollectionAsync(
            string embeddingModel,
            ulong vectorSize,
            CancellationToken cancellationToken = default)
        {
            Calls.Add("ensure");
            LastEmbeddingModel = embeddingModel;
            return Task.CompletedTask;
        }

        public Task EnsureAllCollectionsAsync(
            IEnumerable<(string EmbeddingModel, ulong VectorSize)> models,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task StoreAsync(
            string embeddingModel,
            ulong vectorSize,
            IEnumerable<VectorRecord> records,
            CancellationToken cancellationToken = default)
        {
            Calls.Add("store");
            LastEmbeddingModel = embeddingModel;
            Stored.AddRange(records);
            return Task.CompletedTask;
        }

        public Task DeleteByDocumentAsync(
            string embeddingModel,
            int documentId,
            CancellationToken cancellationToken = default)
        {
            Calls.Add("delete");
            LastEmbeddingModel = embeddingModel;
            DeletedDocumentId = documentId;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SearchHit>> SearchAsync(
            string embeddingModel,
            float[] embedding,
            int topK = 5,
            DocumentChunkFilter? documentFilter = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SearchHit>>([]);

        public string CollectionNameFor(string embeddingModel) => $"rag-notes__{embeddingModel}";
    }
}
