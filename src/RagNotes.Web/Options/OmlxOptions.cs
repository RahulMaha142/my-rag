namespace RagNotes.Web.Options;

public class OmlxOptions
{
    public const string SectionName = "Omlx";

    public string BaseUrl { get; set; } = "http://127.0.0.1:8000";
    public string EmbeddingModel { get; set; } = "bge-m3-mlx-4bit";
    public string ChatModel { get; set; } = "granite-3.3-8b-instruct-6bit";
    public string? ApiKey { get; set; }
}
