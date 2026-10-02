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
            "/api/embed",
            new { model = _options.EmbeddingModel, input = text },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Ollama embeddings failed ({(int)response.StatusCode}): {body}. " +
                $"Model '{_options.EmbeddingModel}' must be pulled (ollama pull {_options.EmbeddingModel}).");
        }

        var result = await response.Content
            .ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken);

        var embedding = result?.Embeddings.FirstOrDefault();
        if (embedding is null || embedding.Length == 0)
        {
            throw new InvalidOperationException(
                "Ollama returned an empty embedding.");
        }

        return embedding;
    }

    private sealed class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embeddings")]
        public float[][] Embeddings { get; set; } = [];
    }
}