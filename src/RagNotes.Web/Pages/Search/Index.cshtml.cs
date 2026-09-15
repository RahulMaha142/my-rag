using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagNotes.Web.Services;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Web.Pages.Search;

public class IndexModel : PageModel
{
    private readonly SemanticSearchService _search;

    public IndexModel(SemanticSearchService search)
    {
        _search = search;
    }

    [BindProperty]
    [Required(ErrorMessage = "Enter a search query.")]
    [Display(Name = "Query")]
    public string Query { get; set; } = string.Empty;

    public IReadOnlyList<SearchHit> Results { get; private set; } = [];

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

        Results = await _search.SearchAsync(Query.Trim(), topK: 5, cancellationToken);
        return Page();
    }
}
