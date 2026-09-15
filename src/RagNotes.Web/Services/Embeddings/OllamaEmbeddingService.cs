using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RagNotes.Web.Options;

namespace RagNotes.Web.Services.Embeddings;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _options;

    public OllamaEmbeddingService(HttpClient http, IOptions<OllamaOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "/api/embeddings",
            new { model = _options.EmbeddingModel, prompt = text },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken);

        if (result?.Embedding is null || result.Embedding.Length == 0)
        {
            throw new InvalidOperationException(
                "Ollama returned an empty embedding.");
        }

        return result.Embedding;
    }

    private sealed class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = [];
    }
}