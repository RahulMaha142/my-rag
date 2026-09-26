namespace RagNotes.Web.Options;

public class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string EmbeddingModel { get; set; } = "bge-m3";
    public string ChatModel { get; set; } = "granite3.3:8b";
}
