using RagNotes.Web.Models;
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
        var store = new RecordingVectorStore();
        var service = new DocumentIndexingService(
            new FixedChunker(chunkCount: 2),
            new StubEmbeddingService(),
            store);

        var count = await service.ReindexAsync(
            documentId: 7,
            fileName: "note.md",
            content: "hello world",
            new ChunkOptions(MaxTokens: 64, OverlapPercent: 10, PreferSentences: false));

        Assert.Equal(2, count);
        Assert.Equal(new[] { "delete", "store" }, store.Calls);
        Assert.Equal(7, store.DeletedDocumentId);
        Assert.Equal(2, store.Stored.Count);
        Assert.All(store.Stored, record =>
        {
            Assert.Equal(7, record.DocumentId);
            Assert.Equal("note.md", record.FileName);
        });
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
        public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
            => Task.FromResult(new[] { 0.1f, 0.2f });
    }

    private sealed class RecordingVectorStore : IVectorStore
    {
        public List<string> Calls { get; } = [];
        public int? DeletedDocumentId { get; private set; }
        public List<VectorRecord> Stored { get; } = [];

        public Task EnsureCollectionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task StoreAsync(
            IEnumerable<VectorRecord> records,
            CancellationToken cancellationToken = default)
        {
            Calls.Add("store");
            Stored.AddRange(records);
            return Task.CompletedTask;
        }

        public Task DeleteByDocumentAsync(int documentId, CancellationToken cancellationToken = default)
        {
            Calls.Add("delete");
            DeletedDocumentId = documentId;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SearchHit>> SearchAsync(
            float[] embedding,
            int topK = 5,
            DocumentChunkFilter? documentFilter = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SearchHit>>([]);
    }
}
