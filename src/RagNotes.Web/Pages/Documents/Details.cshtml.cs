using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagNotes.Web.Data;
using RagNotes.Web.Models;

namespace RagNotes.Web.Pages.Documents;

public class DetailsModel : PageModel
{
    private readonly RagNotesDbContext _db;

    public DetailsModel(RagNotesDbContext db)
    {
        _db = db;
    }

    public Document Document { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var document = await _db.Documents.FindAsync(id);
        if (document is null)
        {
            return NotFound();
        }

        Document = document;
        return Page();
    }
}