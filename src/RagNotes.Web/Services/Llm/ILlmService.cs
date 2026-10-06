namespace RagNotes.Web.Services.Llm;

public interface ILlmService
{
    Task<string> GenerateAsync(
        string prompt,
        string? model = null,
        CancellationToken cancellationToken = default);
}
