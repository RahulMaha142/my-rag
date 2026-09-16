using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagNotes.Web.Services;
using RagNotes.Web.Services.Llm;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Web.Pages.Search;

public class IndexModel : PageModel
{
    private readonly SemanticSearchService _search;
    private readonly ILlmService _llm;

    public IndexModel(SemanticSearchService search, ILlmService llm)
    {
        _search = search;
        _llm = llm;
    }

    [BindProperty]
    [Required(ErrorMessage = "Enter a search query.")]
    [Display(Name = "Query")]
    public string Query { get; set; } = string.Empty;

    public IReadOnlyList<SearchHit> Results { get; private set; } = [];

    public string? Answer { get; private set; }

    public bool HasSearched { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        HasSearched = true;

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Query))
        {
            return Page();
        }

        var question = Query.Trim();
        Results = await _search.SearchAsync(question, topK: 5, cancellationToken);

        if (Results.Count > 0)
        {
            var prompt = RagPromptBuilder.Build(question, Results);
            Answer = await _llm.GenerateAsync(prompt, cancellationToken);
        }

        return Page();
    }
}
