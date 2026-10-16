using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using RagNotes.Web.Options;
using RagNotes.Web.Services.Embeddings;
using static Qdrant.Client.Grpc.Conditions;

namespace RagNotes.Web.Services.VectorStore;

public class QdrantVectorStore : IVectorStore
{
    private static readonly Regex SanitizeRegex = new(
        @"[^a-zA-Z0-9._-]+",
        RegexOptions.Compiled);

    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;
    private readonly IEmbeddingModelCatalog _catalog;
    private readonly object _legacyLock = new();
    private bool? _useLegacyDefaultCollection;

    public QdrantVectorStore(
        QdrantClient client,
        IOptions<QdrantOptions> options,
        IEmbeddingModelCatalog catalog)
    {
        _client = client;
        _options = options.Value;
        _catalog = catalog;
    }

    public static Guid CreatePointId(int documentId, int chunkIndex)
    {
        var bytes = MD5.HashData(
            Encoding.UTF8.GetBytes($"rag-notes:{documentId}:{chunkIndex}"));
        return new Guid(bytes);
    }

    public string CollectionNameFor(string embeddingModel)
    {
        // Prefer the cached legacy decision from Ensure*/Store/Search.
        // Before that runs, assume the suffixed name (safe for display).
        lock (_legacyLock)
        {
            if (_useLegacyDefaultCollection == true &&
                string.Equals(
                    embeddingModel.Trim(),
                    _catalog.DefaultModel,
                    StringComparison.Ordinal))
            {
                return _options.CollectionName;
            }
        }

        return SuffixedCollectionName(embeddingModel);
    }

    public async Task EnsureCollectionAsync(
        string embeddingModel,
        ulong vectorSize,
        CancellationToken cancellationToken = default)
    {
        if (vectorSize == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vectorSize),
                "Vector size must be greater than zero.");
        }

        var collectionName = await ResolveCollectionNameAsync(
            embeddingModel,
            cancellationToken);

        var exists = await _client.CollectionExistsAsync(
            collectionName,
            cancellationToken);

        if (exists)
        {
            return;
        }

        await _client.CreateCollectionAsync(
            collectionName: collectionName,
            vectorsConfig: new VectorParams
            {
                Size = vectorSize,
                Distance = Distance.Cosine
            },
            cancellationToken: cancellationToken);
    }

    public async Task EnsureAllCollectionsAsync(
        IEnumerable<(string EmbeddingModel, ulong VectorSize)> models,
        CancellationToken cancellationToken = default)
    {
        foreach (var (embeddingModel, vectorSize) in models)
        {
            if (string.IsNullOrWhiteSpace(embeddingModel) || vectorSize == 0)
                continue;

            await EnsureCollectionAsync(embeddingModel, vectorSize, cancellationToken);
        }
    }

    public async Task StoreAsync(
        string embeddingModel,
        ulong vectorSize,
        IEnumerable<VectorRecord> records,
        CancellationToken cancellationToken = default)
    {
        var collectionName = await ResolveCollectionNameAsync(
            embeddingModel,
            cancellationToken);

        var points = new List<PointStruct>();
        foreach (var record in records)
        {
            if (record.Vector.Length != (int)vectorSize)
            {
                throw new InvalidOperationException(
                    $"Embedding for model '{embeddingModel}' has {record.Vector.Length} dimensions, " +
                    $"but the collection expects {vectorSize}.");
            }

            points.Add(new PointStruct
            {
                Id = record.Id,
                Vectors = record.Vector,
                Payload =
                {
                    ["documentId"] = record.DocumentId,
                    ["fileName"] = record.FileName,
                    ["chunkIndex"] = record.ChunkIndex,
                    ["text"] = record.Text,
                    ["embeddingModel"] = embeddingModel
                }
            });
        }

        if (points.Count == 0)
        {
            return;
        }

        await _client.UpsertAsync(
            collectionName: collectionName,
            points: points,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteByDocumentAsync(
        string embeddingModel,
        int documentId,
        CancellationToken cancellationToken = default)
    {
        var collectionName = await ResolveCollectionNameAsync(
            embeddingModel,
            cancellationToken);

        var exists = await _client.CollectionExistsAsync(
            collectionName,
            cancellationToken);
        if (!exists)
        {
            return;
        }

        await _client.DeleteAsync(
            collectionName: collectionName,
            filter: Match("documentId", documentId),
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        string embeddingModel,
        float[] embedding,
        int topK = 5,
        DocumentChunkFilter? documentFilter = null,
        CancellationToken cancellationToken = default)
    {
        var expectedSize = _catalog.GetVectorSize(embeddingModel);
        if (embedding.Length != (int)expectedSize)
        {
            throw new InvalidOperationException(
                $"Query embedding for model '{embeddingModel}' has {embedding.Length} dimensions, " +
                $"but the collection expects {expectedSize}.");
        }

        var collectionName = await ResolveCollectionNameAsync(
            embeddingModel,
            cancellationToken);

        var exists = await _client.CollectionExistsAsync(
            collectionName,
            cancellationToken);
        if (!exists)
        {
            return [];
        }

        var points = await _client.QueryAsync(
            collectionName: collectionName,
            query: embedding,
            filter: ToFilter(documentFilter),
            limit: (ulong)topK,
            payloadSelector: true,
            cancellationToken: cancellationToken);

        return points.Select(point => new SearchHit(
            Score: point.Score,
            DocumentId: (int)point.Payload["documentId"].IntegerValue,
            FileName: point.Payload["fileName"].StringValue,
            ChunkIndex: (int)point.Payload["chunkIndex"].IntegerValue,
            Text: point.Payload["text"].StringValue)).ToList();
    }

    private async Task<string> ResolveCollectionNameAsync(
        string embeddingModel,
        CancellationToken cancellationToken)
    {
        var defaultModel = _catalog.DefaultModel;
        var isDefault = string.Equals(
            embeddingModel.Trim(),
            defaultModel,
            StringComparison.Ordinal);

        if (isDefault && await UseLegacyDefaultCollectionAsync(cancellationToken))
            return _options.CollectionName;

        return SuffixedCollectionName(embeddingModel);
    }

    private async Task<bool> UseLegacyDefaultCollectionAsync(
        CancellationToken cancellationToken)
    {
        lock (_legacyLock)
        {
            if (_useLegacyDefaultCollection.HasValue)
                return _useLegacyDefaultCollection.Value;
        }

        var legacyExists = await _client.CollectionExistsAsync(
            _options.CollectionName,
            cancellationToken);
        var suffixedExists = await _client.CollectionExistsAsync(
            SuffixedCollectionName(_catalog.DefaultModel),
            cancellationToken);

        var useLegacy = legacyExists && !suffixedExists;
        lock (_legacyLock)
        {
            _useLegacyDefaultCollection = useLegacy;
        }

        return useLegacy;
    }

    private string SuffixedCollectionName(string embeddingModel) =>
        $"{_options.CollectionName}__{SanitizeModelId(embeddingModel)}";

    internal static string SanitizeModelId(string embeddingModel)
    {
        var trimmed = embeddingModel.Trim().ToLowerInvariant();
        var sanitized = SanitizeRegex.Replace(trimmed, "-").Trim('-', '_', '.');
        return string.IsNullOrEmpty(sanitized) ? "model" : sanitized;
    }

    private static Filter? ToFilter(DocumentChunkFilter? documentFilter)
    {
        if (documentFilter is null || documentFilter.DocumentIds.Count == 0)
            return null;

        var filter = new Filter();
        foreach (var id in documentFilter.DocumentIds)
        {
            var condition = Match("documentId", id);
            if (documentFilter.Mode == DocumentFilterMode.Exclude)
                filter.MustNot.Add(condition);
            else
                filter.Should.Add(condition);
        }

        return filter;
    }
}
