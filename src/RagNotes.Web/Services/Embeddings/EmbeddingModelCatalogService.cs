using Microsoft.Extensions.Options;
using RagNotes.Web.Options;

namespace RagNotes.Web.Services.Embeddings;

public sealed class EmbeddingModelCatalogService : IEmbeddingModelCatalog
{
    private readonly string _defaultModel;
    private readonly IReadOnlyList<EmbeddingModelOption> _available;

    public EmbeddingModelCatalogService(
        IOptions<InferenceOptions> inference,
        IOptions<OmlxOptions> omlx,
        IOptions<OllamaOptions> ollama)
    {
        var provider = inference.Value.Provider?.Trim() ?? "Omlx";
        if (string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase))
        {
            _defaultModel = ollama.Value.EmbeddingModel;
            _available = ollama.Value.AvailableEmbeddingModels;
        }
        else
        {
            _defaultModel = omlx.Value.EmbeddingModel;
            _available = omlx.Value.AvailableEmbeddingModels;
        }
    }

    public string DefaultModel =>
        string.IsNullOrWhiteSpace(_defaultModel)
            ? (_available.FirstOrDefault()?.Id ?? string.Empty)
            : _defaultModel.Trim();

    public IReadOnlyList<EmbeddingModelOption> AvailableModels => _available;

    public string Resolve(string? requested) =>
        EmbeddingModelCatalog.Resolve(requested, DefaultModel, _available);

    public ulong GetVectorSize(string modelId)
    {
        var size = EmbeddingModelCatalog.GetVectorSize(modelId, _available);
        if (size == 0)
        {
            throw new InvalidOperationException(
                $"Embedding model '{modelId}' has no configured VectorSize.");
        }

        return size;
    }
}
