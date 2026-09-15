using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using RagNotes.Web.Options;
using static Qdrant.Client.Grpc.Conditions;

namespace RagNotes.Web.Services.VectorStore;

public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;

    public QdrantVectorStore(QdrantClient client, IOptions<QdrantOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public static Guid CreatePointId(int documentId, int chunkIndex)
    {
        var bytes = MD5.HashData(
            Encoding.UTF8.GetBytes($"rag-notes:{documentId}:{chunkIndex}"));
        return new Guid(bytes);
    }

    public async Task EnsureCollectionAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _client.CollectionExistsAsync(
            _options.CollectionName,
            cancellationToken);

        if (exists)
        {
            return;
        }

        await _client.CreateCollectionAsync(
            collectionName: _options.CollectionName,
            vectorsConfig: new VectorParams
            {
                Size = _options.VectorSize,
                Distance = Distance.Cosine
            },
            cancellationToken: cancellationToken);
    }

    public async Task StoreAsync(
        IEnumerable<VectorRecord> records,
        CancellationToken cancellationToken = default)
    {
        var points = records.Select(record => new PointStruct
        {
            Id = record.Id,
            Vectors = record.Vector,
            Payload =
            {
                ["documentId"] = record.DocumentId,
                ["fileName"] = record.FileName,
                ["chunkIndex"] = record.ChunkIndex,
                ["text"] = record.Text
            }
        }).ToList();

        if (points.Count == 0)
        {
            return;
        }

        await _client.UpsertAsync(
            collectionName: _options.CollectionName,
            points: points,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteByDocumentAsync(
        int documentId,
        CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync(
            collectionName: _options.CollectionName,
            filter: Match("documentId", documentId),
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        float[] embedding,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var points = await _client.QueryAsync(
            collectionName: _options.CollectionName,
            query: embedding,
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
}
