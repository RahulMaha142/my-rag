using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagNotes.Web.Data;
using RagNotes.Web.Models;
using RagNotes.Web.Services.Chunking;

namespace RagNotes.Web.Pages.Documents;

public class DetailsModel : PageModel
{
    private const int DefaultPageSize = 10;
    private const int ContentPreviewLength = 2000;

    private readonly RagNotesDbContext _db;
    private readonly ITextChunker _chunker;

    public DetailsModel(RagNotesDbContext db, ITextChunker chunker)
    {
        _db = db;
        _chunker = chunker;
    }

    public Document Document { get; private set; } = null!;
    public string DisplayContent { get; private set; } = string.Empty;
    public bool ContentIsTruncated { get; private set; }
    public bool ShowFullContent { get; private set; }
    public IReadOnlyList<TextChunk> Chunks { get; private set; } = [];
    public int PageNumber { get; private set; }
    public int PageSize { get; private set; } = DefaultPageSize;
    public int TotalChunks { get; private set; }
    public int TotalPages { get; private set; }

    public async Task<IActionResult> OnGetAsync(
        int id,
        int pageNumber = 1,
        bool showFullContent = false,
        CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents.FindAsync([id], cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        Document = document;
        ShowFullContent = showFullContent;

        var content = document.Content ?? string.Empty;
        ContentIsTruncated = content.Length > ContentPreviewLength;
        DisplayContent = ShowFullContent || !ContentIsTruncated
            ? content
            : content[..ContentPreviewLength];

        var allChunks = _chunker.Chunk(content, document.Id, document.ChunkOptions);
        TotalChunks = allChunks.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalChunks / (double)PageSize));
        PageNumber = Math.Clamp(pageNumber, 1, TotalPages);

        var skip = (PageNumber - 1) * PageSize;
        Chunks = allChunks.Skip(skip).Take(PageSize).ToList();

        return Page();
    }
}
