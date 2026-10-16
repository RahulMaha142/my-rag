using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RagNotes.Web.Data;
using RagNotes.Web.Models;
using RagNotes.Web.Options;
using RagNotes.Web.Services;
using RagNotes.Web.Services.Chunking;
using RagNotes.Web.Services.Embeddings;

namespace RagNotes.Web.Pages.Documents;

public class DetailsModel : PageModel
{
    private const int DefaultPageSize = 10;
    private const int ContentPreviewLength = 2000;

    private readonly RagNotesDbContext _db;
    private readonly ITextChunker _chunker;
    private readonly DocumentIndexingService _indexing;
    private readonly IEmbeddingModelCatalog _embeddingModels;

    public DetailsModel(
        RagNotesDbContext db,
        ITextChunker chunker,
        DocumentIndexingService indexing,
        IEmbeddingModelCatalog embeddingModels)
    {
        _db = db;
        _chunker = chunker;
        _indexing = indexing;
        _embeddingModels = embeddingModels;
    }

    public Document Document { get; private set; } = null!;
    public string DisplayContent { get; private set; } = string.Empty;
    public bool ContentIsTruncated { get; private set; }
    public bool ShowFullContent { get; private set; }
    public IReadOnlyList<TextChunk> Chunks { get; private set; } = [];
    public IReadOnlyList<DocumentEmbeddingIndex> Indexes { get; private set; } = [];
    public IReadOnlyList<EmbeddingModelOption> MissingEmbeddingModels { get; private set; } = [];
    public int PageNumber { get; private set; }
    public int PageSize { get; private set; } = DefaultPageSize;
    public int TotalChunks { get; private set; }
    public int TotalPages { get; private set; }

    [BindProperty]
    [Display(Name = "Embedding model")]
    public string EmbeddingModel { get; set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(
        int id,
        int pageNumber = 1,
        bool showFullContent = false,
        CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents
            .Include(item => item.EmbeddingIndexes)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        BindDocument(document, pageNumber, showFullContent);
        return Page();
    }

    public async Task<IActionResult> OnPostReindexAsync(
        int id,
        int maxTokens,
        int overlapPercent,
        bool preferSentences,
        CancellationToken cancellationToken)
    {
        var document = await _db.Documents
            .Include(item => item.EmbeddingIndexes)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
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

            StatusMessage =
                $"Reindexed \"{document.FileName}\" across attached embedding models ({chunkCount} chunk vectors written).";
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            StatusMessage =
                $"Saved the new chunking settings for \"{document.FileName}\", but reindexing failed: {ex.Message} Submit the form again to retry.";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostIndexIntoModelAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var document = await _db.Documents
            .Include(item => item.EmbeddingIndexes)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var model = _embeddingModels.Resolve(EmbeddingModel);
        if (document.EmbeddingIndexes.Any(index =>
                string.Equals(index.EmbeddingModel, model, StringComparison.Ordinal)))
        {
            StatusMessage = $"\"{document.FileName}\" is already indexed with {model}.";
            return RedirectToPage(new { id });
        }

        try
        {
            var chunkCount = await _indexing.IndexIntoModelAsync(document, model, cancellationToken);
            StatusMessage = $"Indexed \"{document.FileName}\" into {model} ({chunkCount} chunks).";
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            StatusMessage = $"Indexing into {model} failed: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveFromModelAsync(
        int id,
        string embeddingModel,
        CancellationToken cancellationToken)
    {
        var document = await _db.Documents.FindAsync([id], cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var model = _embeddingModels.Resolve(embeddingModel);
        await _indexing.RemoveFromModelAsync(id, model, cancellationToken);
        StatusMessage = $"Removed \"{document.FileName}\" from {model}.";
        return RedirectToPage(new { id });
    }

    private void BindDocument(Document document, int pageNumber, bool showFullContent)
    {
        Document = document;
        ShowFullContent = showFullContent;
        Indexes = document.EmbeddingIndexes
            .OrderBy(index => index.EmbeddingModel)
            .ToList();

        var indexed = Indexes
            .Select(index => index.EmbeddingModel)
            .ToHashSet(StringComparer.Ordinal);
        MissingEmbeddingModels = _embeddingModels.AvailableModels
            .Where(model => !indexed.Contains(model.Id))
            .ToList();
        EmbeddingModel = MissingEmbeddingModels.FirstOrDefault()?.Id
            ?? _embeddingModels.DefaultModel;

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
    }
}
