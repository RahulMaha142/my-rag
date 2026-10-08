namespace RagNotes.Web.Services.Llm;

public interface ILlmService
{
    Task<string> GenerateAsync(
        string prompt,
        string? model = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<LlmEvent> StreamAsync(
        string prompt,
        string? model,
        bool enableThinking,
        CancellationToken cancellationToken = default);
}
