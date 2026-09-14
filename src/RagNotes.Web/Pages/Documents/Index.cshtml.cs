using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RagNotes.Web.Data;
using RagNotes.Web.Models;

namespace RagNotes.Web.Pages.Documents;

public class IndexModel : PageModel
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".txt", ".md" };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly RagNotesDbContext _db;

    public IndexModel(RagNotesDbContext db)
    {
        _db = db;
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

    public async Task<IActionResult> OnPostAsync(IFormFile? file)
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

        string content;
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            content = await reader.ReadToEndAsync();
        }

        var document = new Document
        {
            FileName = Path.GetFileName(file.FileName),
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _db.Documents.Add(document);
        await _db.SaveChangesAsync();

        StatusMessage = $"Uploaded \"{document.FileName}\".";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var document = await _db.Documents.FindAsync(id);
        if (document is not null)
        {
            _db.Documents.Remove(document);
            await _db.SaveChangesAsync();
            StatusMessage = $"Deleted \"{document.FileName}\".";
        }

        return RedirectToPage();
    }
}