using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagNotes.Web.Options;
using RagNotes.Web.Services.Embeddings;
using RagNotes.Web.Services.Evaluation;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Web.Pages.Evaluation;

public class IndexModel : PageModel
{
    public const string EmbeddingModelCookieName = "rag-embedding-model";

    private readonly EvalQuestionCatalog _catalog;
    private readonly EvaluationRunner _runner;
    private readonly IEmbeddingModelCatalog _embeddingModels;

    public IndexModel(
        EvalQuestionCatalog catalog,
        EvaluationRunner runner,
        IEmbeddingModelCatalog embeddingModels)
    {
        _catalog = catalog;
        _runner = runner;
        _embeddingModels = embeddingModels;
    }

    [BindProperty]
    [Display(Name = "Embedding model")]
    public string EmbeddingModel { get; set; } = string.Empty;

    public IReadOnlyList<EmbeddingModelOption> EmbeddingModels { get; private set; } = [];

    public int AnswerableCount { get; private set; }

    public int DistractorCount { get; private set; }

    public bool HasRun { get; private set; }

    public EvalRunResult? Result { get; private set; }

    public void OnGet()
    {
        EmbeddingModel = _embeddingModels.Resolve(Request.Cookies[EmbeddingModelCookieName]);
        LoadEmbeddingModels();
        LoadCounts();
    }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        HasRun = true;
        EmbeddingModel = _embeddingModels.Resolve(EmbeddingModel);
        RememberEmbeddingModel(EmbeddingModel);
        LoadEmbeddingModels();
        LoadCounts();
        Result = await _runner.RunAsync(EmbeddingModel, cancellationToken);
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

    private void LoadEmbeddingModels()
    {
        var models = _embeddingModels.AvailableModels.ToList();
        if (!string.IsNullOrEmpty(EmbeddingModel) &&
            !models.Exists(model => string.Equals(model.Id, EmbeddingModel, StringComparison.Ordinal)))
        {
            models.Insert(0, new EmbeddingModelOption { Id = EmbeddingModel, VectorSize = 0 });
        }

        EmbeddingModels = models;
    }

    private void RememberEmbeddingModel(string model)
    {
        if (string.IsNullOrEmpty(model))
            return;

        Response.Cookies.Append(EmbeddingModelCookieName, model, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(30),
            Path = "/"
        });
    }
}
