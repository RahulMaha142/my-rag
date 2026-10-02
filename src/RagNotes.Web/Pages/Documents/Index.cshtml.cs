using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RagNotes.Web.Data;
using RagNotes.Web.Models;
using RagNotes.Web.Services;

namespace RagNotes.Web.Pages.Documents;

public class IndexModel : PageModel
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".txt", ".md" };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly RagNotesDbContext _db;
    private readonly DocumentIndexingService _indexing;

    public IndexModel(RagNotesDbContext db, DocumentIndexingService indexing)
    {
        _db = db;
        _indexing = indexing;
    }

    public List<Document> Documents { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Documents = await _db.Documents
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(
        IFormFile? file,
        int maxTokens,
        int overlapPercent,
        bool preferSentences,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            StatusMessage = "Please choose a non-empty file.";
            return RedirectToPage();
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            StatusMessage = "Only .txt and .md files are allowed.";
            return RedirectToPage();
        }

        if (file.Length > MaxFileSizeBytes)
        {
            StatusMessage = "File is too large (max 5 MB).";
            return RedirectToPage();
        }

        var settingsError = ChunkOptions.Validate(maxTokens, overlapPercent);
        if (settingsError is not null)
        {
            StatusMessage = settingsError;
            return RedirectToPage();
        }

        var options = new ChunkOptions(maxTokens, overlapPercent, preferSentences);

        string content;
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            content = await reader.ReadToEndAsync(cancellationToken);
        }

        var document = new Document
        {
            FileName = Path.GetFileName(file.FileName),
            Content = content,
            CreatedAt = DateTime.UtcNow,
            MaxTokens = options.MaxTokens,
            OverlapPercent = options.OverlapPercent,
            PreferSentences = options.PreferSentences
        };

        _db.Documents.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        var chunkCount = await _indexing.IndexAsync(
            document.Id,
            document.FileName,
            document.Content,
            document.ChunkOptions,
            cancellationToken);

        StatusMessage = $"Uploaded \"{document.FileName}\" ({chunkCount} chunks indexed).";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var document = await _db.Documents.FindAsync([id], cancellationToken);
        if (document is not null)
        {
            await _indexing.DeleteAsync(id, cancellationToken);
            _db.Documents.Remove(document);
            await _db.SaveChangesAsync(cancellationToken);
            StatusMessage = $"Deleted \"{document.FileName}\".";
        }

        return RedirectToPage();
    }
}
