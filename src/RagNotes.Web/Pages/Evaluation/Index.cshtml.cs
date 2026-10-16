using Microsoft.AspNetCore.Mvc.RazorPages;
using RagNotes.Web.Services.Evaluation;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Web.Pages.Evaluation;

public class IndexModel : PageModel
{
    private readonly EvalQuestionCatalog _catalog;
    private readonly EvaluationRunner _runner;

    public IndexModel(EvalQuestionCatalog catalog, EvaluationRunner runner)
    {
        _catalog = catalog;
        _runner = runner;
    }

    public int AnswerableCount { get; private set; }

    public int DistractorCount { get; private set; }

    public bool HasRun { get; private set; }

    public EvalRunResult? Result { get; private set; }

    public void OnGet()
    {
        LoadCounts();
    }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        HasRun = true;
        LoadCounts();
        Result = await _runner.RunAsync(cancellationToken);
    }

    public static string Snippet(SearchHit hit, int maxLength = 240)
    {
        var text = EvalScorer.CollapseWhitespace(hit.Text);
        if (text.Length <= maxLength)
            return text;

        return text[..maxLength] + "…";
    }

    public static string OutcomeLabel(EvalOutcome outcome) => outcome switch
    {
        EvalOutcome.Passed => "Pass",
        EvalOutcome.Failed => "Fail",
        EvalOutcome.NotIndexed => "Not indexed",
        _ => "Distractor"
    };

    private void LoadCounts()
    {
        var questions = _catalog.Questions;
        AnswerableCount = questions.Count(question => question.Answerable);
        DistractorCount = questions.Count(question => !question.Answerable);
    }
}
