using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RagNotes.Web.Options;

namespace RagNotes.Web.Services.Embeddings;

public class OmlxEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly OmlxOptions _options;

    public OmlxEmbeddingService(HttpClient http, IOptions<OmlxOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "/v1/embeddings",
            new
            {
                model = _options.EmbeddingModel,
                input = text,
                encoding_format = "float"
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"oMLX embeddings failed ({(int)response.StatusCode}): {body}. " +
                $"Model '{_options.EmbeddingModel}' must be loaded in oMLX.");
        }

        var result = await response.Content
            .ReadFromJsonAsync<EmbeddingResponse>(cancellationToken);

        var embedding = result?.Data.FirstOrDefault()?.Embedding;
        if (embedding is null || embedding.Length == 0)
        {
            throw new InvalidOperationException(
                "oMLX returned an empty embedding.");
        }

        return embedding;
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public EmbeddingItem[] Data { get; set; } = [];
    }

    private sealed class EmbeddingItem
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = [];
    }
}
