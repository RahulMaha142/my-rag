using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagNotes.Web.Data;
using RagNotes.Web.Models;
using RagNotes.Web.Services;
using RagNotes.Web.Services.Chunking;

namespace RagNotes.Web.Pages.Documents;

public class DetailsModel : PageModel
{
    private const int DefaultPageSize = 10;
    private const int ContentPreviewLength = 2000;

    private readonly RagNotesDbContext _db;
    private readonly ITextChunker _chunker;
    private readonly DocumentIndexingService _indexing;

    public DetailsModel(RagNotesDbContext db, ITextChunker chunker, DocumentIndexingService indexing)
    {
        _db = db;
        _chunker = chunker;
        _indexing = indexing;
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

    [TempData]
    public string? StatusMessage { get; set; }

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

    public async Task<IActionResult> OnPostReindexAsync(
        int id,
        int maxTokens,
        int overlapPercent,
        bool preferSentences,
        CancellationToken cancellationToken)
    {
        var document = await _db.Documents.FindAsync([id], cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var settingsError = ChunkOptions.Validate(maxTokens, overlapPercent);
        if (settingsError is not null)
        {
            StatusMessage = settingsError;
            return RedirectToPage(new { id });
        }

        var options = new ChunkOptions(maxTokens, overlapPercent, preferSentences);
        if (options == document.ChunkOptions)
        {
            StatusMessage = "Chunking settings are unchanged.";
            return RedirectToPage(new { id });
        }

        document.MaxTokens = options.MaxTokens;
        document.OverlapPercent = options.OverlapPercent;
        document.PreferSentences = options.PreferSentences;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var chunkCount = await _indexing.ReindexAsync(
                document.Id,
                document.FileName,
                document.Content,
                document.ChunkOptions,
                cancellationToken);

            StatusMessage = $"Reindexed \"{document.FileName}\" ({chunkCount} chunks).";
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            StatusMessage =
                $"Saved the new chunking settings for \"{document.FileName}\", but reindexing failed: {ex.Message} Submit the form again to retry.";
        }

        return RedirectToPage(new { id });
    }
}
