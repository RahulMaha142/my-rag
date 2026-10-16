using Microsoft.EntityFrameworkCore;
using RagNotes.Web.Data;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Web.Services.Evaluation;

public sealed class EvaluationRunner
{
    public const int TopK = 5;

    private readonly EvalQuestionCatalog _catalog;
    private readonly RagNotesDbContext _db;
    private readonly SemanticSearchService _search;

    public EvaluationRunner(
        EvalQuestionCatalog catalog,
        RagNotesDbContext db,
        SemanticSearchService search)
    {
        _catalog = catalog;
        _db = db;
        _search = search;
    }

    public async Task<EvalRunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var questions = _catalog.Questions;
        var corpusNames = _catalog.CorpusFileNames;
        var documents = await _db.Documents
            .Select(document => new { document.Id, document.FileName })
            .ToListAsync(cancellationToken);

        var indexed = documents
            .Where(document => corpusNames.Contains(document.FileName, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var indexedNames = new HashSet<string>(
            indexed.Select(document => document.FileName),
            StringComparer.OrdinalIgnoreCase);
        var missing = corpusNames
            .Where(name => !indexedNames.Contains(name))
            .ToList();

        var filter = DocumentChunkFilter.FromSelection(
            DocumentChunkFilter.IncludeValue,
            indexed.Select(document => document.Id));

        var answerable = new List<EvalQuestionResult>();
        var distractors = new List<EvalQuestionResult>();

        foreach (var question in questions)
        {
            if (!question.Answerable)
            {
                var distractorHits = filter is null
                    ? Array.Empty<SearchHit>()
                    : await _search.SearchAsync(question.Question, TopK, filter, cancellationToken);
                distractors.Add(new EvalQuestionResult(question, EvalOutcome.Distractor, distractorHits));
                continue;
            }

            if (question.ExpectedFiles.Count == 0 ||
                question.ExpectedFiles.Exists(file => !indexedNames.Contains(file)))
            {
                answerable.Add(new EvalQuestionResult(question, EvalOutcome.NotIndexed, []));
                continue;
            }

            var hits = await _search.SearchAsync(
                question.Question,
                TopK,
                filter,
                cancellationToken);
            var outcome = EvalScorer.Matches(question, hits)
                ? EvalOutcome.Passed
                : EvalOutcome.Failed;
            answerable.Add(new EvalQuestionResult(question, outcome, hits));
        }

        return new EvalRunResult
        {
            MissingFiles = missing,
            Answerable = answerable,
            Distractors = distractors
        };
    }
}
