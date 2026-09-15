using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagNotes.Web.Data;
using RagNotes.Web.Models;
using RagNotes.Web.Services.Chunking;

namespace RagNotes.Web.Pages.Documents;

public class DetailsModel : PageModel
{
    private readonly RagNotesDbContext _db;
    private readonly ITextChunker _chunker;

    public DetailsModel(RagNotesDbContext db, ITextChunker chunker)
    {
        _db = db;
        _chunker = chunker;
    }

    public Document Document { get; private set; } = null!;
    public IReadOnlyList<TextChunk> Chunks { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var document = await _db.Documents.FindAsync(id);
        if (document is null)
        {
            return NotFound();
        }

        Document = document;
        Chunks = _chunker.Chunk(document.Content, document.Id);
        return Page();
    }
}