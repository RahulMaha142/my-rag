namespace RagNotes.Web.Services.Evaluation;

public sealed class EvalQuestion
{
    public string Id { get; init; } = string.Empty;

    public string Question { get; init; } = string.Empty;

    public List<string> ExpectedFiles { get; init; } = [];

    public string? MustContain { get; init; }

    public bool Answerable { get; init; }
}
