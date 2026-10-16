using RagNotes.Web.Options;

namespace RagNotes.Web.Services.Embeddings;

public interface IEmbeddingModelCatalog
{
    string DefaultModel { get; }

    IReadOnlyList<EmbeddingModelOption> AvailableModels { get; }

    string Resolve(string? requested);

    ulong GetVectorSize(string modelId);
}
