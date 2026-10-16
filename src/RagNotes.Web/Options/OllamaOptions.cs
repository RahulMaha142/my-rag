namespace RagNotes.Web.Options;

public class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string EmbeddingModel { get; set; } = "bge-m3";
    public EmbeddingModelOption[] EmbeddingModels { get; set; } =
    [
        new() { Id = "bge-m3", VectorSize = 1024 }
    ];
    public string ChatModel { get; set; } = "granite3.3:8b";

    public IReadOnlyList<EmbeddingModelOption> AvailableEmbeddingModels =>
        EmbeddingModelCatalog.Available(EmbeddingModel, EmbeddingModels);

    public string ResolveEmbeddingModel(string? requested) =>
        EmbeddingModelCatalog.Resolve(requested, EmbeddingModel, AvailableEmbeddingModels);

    public ulong GetVectorSize(string modelId) =>
        EmbeddingModelCatalog.GetVectorSize(modelId, AvailableEmbeddingModels);
}
