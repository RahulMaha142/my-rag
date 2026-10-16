using RagNotes.Web.Services.Evaluation;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Tests;

public class EvalScorerTests
{
    [Fact]
    public void Passes_when_expected_file_contains_the_quote()
    {
        var question = Question("Federalist No. 51.txt", "If men were angels");
        var hits = new[]
        {
            Hit("Federalist No. 10.txt", "The violence of faction"),
            Hit("Federalist No. 51.txt", "If men were angels, no government would be necessary.")
        };

        Assert.True(EvalScorer.Matches(question, hits));
    }

    [Fact]
    public void Fails_when_the_quote_is_only_in_a_different_file()
    {
        var question = Question("Federalist No. 51.txt", "If men were angels");
        var hits = new[]
        {
            Hit("Federalist No. 10.txt", "If men were angels, no government would be necessary.")
        };

        Assert.False(EvalScorer.Matches(question, hits));
    }

    [Fact]
    public void Ignores_extra_whitespace_in_the_retrieved_passage()
    {
        var question = Question("Federalist No. 1.txt", "reflection and choice");
        var hits = new[]
        {
            Hit("Federalist No. 1.txt", "good government from reflection and\nchoice, or whether")
        };

        Assert.True(EvalScorer.Matches(question, hits));
    }

    private static EvalQuestion Question(string fileName, string quote) => new()
    {
        Id = "sample",
        Question = "sample",
        ExpectedFiles = [fileName],
        MustContain = quote,
        Answerable = true
    };

    private static SearchHit Hit(string fileName, string text) => new(
        Score: 0.8f,
        DocumentId: 1,
        FileName: fileName,
        ChunkIndex: 0,
        Text: text);
}
