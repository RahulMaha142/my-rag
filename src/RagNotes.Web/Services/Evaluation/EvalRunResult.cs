using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Web.Services.Evaluation;

public enum EvalOutcome
{
    Passed,
    Failed,
    NotIndexed,
    Distractor
}

public sealed record EvalQuestionResult(
    EvalQuestion Question,
    EvalOutcome Outcome,
    IReadOnlyList<SearchHit> Hits);

public sealed class EvalRunResult
{
    public required IReadOnlyList<string> MissingFiles { get; init; }

    public required IReadOnlyList<EvalQuestionResult> Answerable { get; init; }

    public required IReadOnlyList<EvalQuestionResult> Distractors { get; init; }

    public int PassCount => Answerable.Count(result => result.Outcome == EvalOutcome.Passed);

    public int ScoredCount => Answerable.Count(result =>
        result.Outcome is EvalOutcome.Passed or EvalOutcome.Failed);

    public int NotIndexedCount => Answerable.Count(result => result.Outcome == EvalOutcome.NotIndexed);
}
