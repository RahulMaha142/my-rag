using System.Text.Json;
using System.Text.Json.Serialization;

namespace RagNotes.Web.Services.Llm;

public static class OmlxChatStreamParser
{
    public readonly record struct ParseStep(IReadOnlyList<LlmEvent> Events, bool Stop)
    {
        public static ParseStep Continue { get; } = new([], false);
    }

    public static IReadOnlyList<LlmEvent> Parse(string sse)
    {
        var events = new List<LlmEvent>();
        foreach (var line in sse.Split('\n'))
        {
            var step = ParseLine(line);
            events.AddRange(step.Events);
            if (step.Stop)
                break;
        }

        return events;
    }

    public static ParseStep ParseLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return ParseStep.Continue;

        var trimmed = line.Trim();
        if (!trimmed.StartsWith("data:", StringComparison.Ordinal))
            return ParseStep.Continue;

        var data = trimmed["data:".Length..].Trim();
        if (data.Length == 0)
            return ParseStep.Continue;

        if (data == "[DONE]")
            return new ParseStep([new LlmCompletedEvent(null)], Stop: true);

        ChatChunk? chunk;
        try
        {
            chunk = JsonSerializer.Deserialize<ChatChunk>(data);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "oMLX returned a chat chunk that could not be read.",
                ex);
        }

        var choice = chunk?.Choices?.FirstOrDefault();
        if (choice is null)
            return ParseStep.Continue;

        var events = new List<LlmEvent>();
        var reasoning = choice.Delta?.ReasoningContent;
        if (!string.IsNullOrEmpty(reasoning))
            events.Add(new LlmTextEvent(LlmTextKind.Thinking, reasoning));

        var content = choice.Delta?.Content;
        if (!string.IsNullOrEmpty(content))
            events.Add(new LlmTextEvent(LlmTextKind.Answer, content));

        if (!string.IsNullOrEmpty(choice.FinishReason))
        {
            events.Add(new LlmCompletedEvent(choice.FinishReason));
            return new ParseStep(events, Stop: true);
        }

        return new ParseStep(events, Stop: false);
    }

    private sealed class ChatChunk
    {
        [JsonPropertyName("choices")]
        public ChatChoice[]? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("delta")]
        public ChatDelta? Delta { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private sealed class ChatDelta
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("reasoning_content")]
        public string? ReasoningContent { get; set; }
    }
}
