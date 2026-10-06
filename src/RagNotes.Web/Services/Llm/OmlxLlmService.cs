using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RagNotes.Web.Options;

namespace RagNotes.Web.Services.Llm;

public class OmlxLlmService : ILlmService
{
    private readonly HttpClient _http;
    private readonly OmlxOptions _options;

    public OmlxLlmService(HttpClient http, IOptions<OmlxOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<string> GenerateAsync(
        string prompt,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var chatModel = _options.ResolveChatModel(model);
        var response = await _http.PostAsJsonAsync(
            "/v1/chat/completions",
            new
            {
                model = chatModel,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                stream = false,
                chat_template_kwargs = new { enable_thinking = false }
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"oMLX chat failed ({(int)response.StatusCode}): {body}. " +
                $"Model '{chatModel}' must be loaded in oMLX.");
        }

        var result = await response.Content
            .ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken);

        var text = result?.Choices.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(
                "oMLX returned an empty generation.");
        }

        return text.Trim();
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public ChatChoice[] Choices { get; set; } = [];
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}
