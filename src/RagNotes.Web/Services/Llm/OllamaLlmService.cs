using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RagNotes.Web.Options;

namespace RagNotes.Web.Services.Llm;

public class OllamaLlmService : ILlmService
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _options;

    public OllamaLlmService(HttpClient http, IOptions<OllamaOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<string> GenerateAsync(
        string prompt,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        // qwen3.* models default to chain-of-thought ("thinking").
        // Without think:false they often exhaust tokens before writing response.
        var response = await _http.PostAsJsonAsync(
            "/api/generate",
            new
            {
                model = _options.ChatModel,
                prompt,
                stream = false,
                think = false
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken);

        var text = FirstNonEmpty(result?.Response, result?.Thinking);
        if (text is null)
        {
            throw new InvalidOperationException(
                "Ollama returned an empty generation.");
        }

        return text;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private sealed class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        [JsonPropertyName("thinking")]
        public string? Thinking { get; set; }
    }
}
