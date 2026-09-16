namespace RagNotes.Web.Services.Llm;

public interface ILlmService
{
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}
