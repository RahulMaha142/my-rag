using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RagNotes.Web.Data;
using RagNotes.Web.Models;
using RagNotes.Web.Options;
using RagNotes.Web.Services;
using RagNotes.Web.Services.Embeddings;

namespace RagNotes.Web.Pages.Documents;

public class IndexModel : PageModel
{
    public const string EmbeddingModelCookieName = "rag-embedding-model";

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".txt", ".md" };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly RagNotesDbContext _db;
    private readonly DocumentIndexingService _indexing;
    private readonly IEmbeddingModelCatalog _embeddingModels;

    public IndexModel(
        RagNotesDbContext db,
        DocumentIndexingService indexing,
        IEmbeddingModelCatalog embeddingModels)
    {
        _db = db;
        _indexing = indexing;
        _embeddingModels = embeddingModels;
    }

    public List<DocumentListItem> Documents { get; private set; } = [];

    public IReadOnlyList<EmbeddingModelOption> EmbeddingModels { get; private set; } = [];

    [BindProperty]
    [Display(Name = "Embedding model")]
    public string EmbeddingModel { get; set; } = string.Empty;

    [BindProperty]
    [Display(Name = "Bulk embedding model")]
    public string BulkEmbeddingModel { get; set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        EmbeddingModel = _embeddingModels.Resolve(Request.Cookies[EmbeddingModelCookieName]);
        BulkEmbeddingModel = EmbeddingModel;
        LoadEmbeddingModels();
        await LoadDocumentsAsync();
    }

    public async Task<IActionResult> OnPostAsync(
        IFormFile? file,
        int maxTokens,
        int overlapPercent,
        bool preferSentences,
        CancellationToken cancellationToken)
    {
        EmbeddingModel = _embeddingModels.Resolve(EmbeddingModel);
        RememberEmbeddingModel(EmbeddingModel);
        LoadEmbeddingModels();

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

        try
        {
            var chunkCount = await _indexing.IndexAsync(
                document.Id,
                document.FileName,
                document.Content,
                document.ChunkOptions,
                EmbeddingModel,
                cancellationToken);

            StatusMessage =
                $"Uploaded \"{document.FileName}\" ({chunkCount} chunks indexed with {EmbeddingModel}).";
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            StatusMessage =
                $"Saved \"{document.FileName}\", but indexing failed: {ex.Message} " +
                "Open the document and index it into an embedding model to retry.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostIndexMissingAsync(CancellationToken cancellationToken)
    {
        var model = _embeddingModels.Resolve(BulkEmbeddingModel);
        RememberEmbeddingModel(model);

        var documents = await _db.Documents
            .Include(document => document.EmbeddingIndexes)
            .OrderBy(document => document.FileName)
            .ToListAsync(cancellationToken);

        var missing = documents
            .Where(document => document.EmbeddingIndexes.All(index =>
                !string.Equals(index.EmbeddingModel, model, StringComparison.Ordinal)))
            .ToList();

        if (missing.Count == 0)
        {
            StatusMessage = $"Every document is already indexed with {model}.";
            return RedirectToPage();
        }

        var succeeded = 0;
        var failed = 0;
        string? firstError = null;

        foreach (var document in missing)
        {
            try
            {
                await _indexing.IndexIntoModelAsync(document, model, cancellationToken);
                succeeded++;
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                failed++;
                firstError ??= ex.Message;
            }
        }

        StatusMessage = failed == 0
            ? $"Indexed {succeeded} document(s) into {model}."
            : $"Indexed {succeeded} document(s) into {model}; {failed} failed. First error: {firstError}";

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

    private async Task LoadDocumentsAsync()
    {
        Documents = await _db.Documents
            .AsNoTracking()
            .OrderByDescending(document => document.CreatedAt)
            .Select(document => new DocumentListItem(
                document.Id,
                document.FileName,
                document.CreatedAt,
                document.MaxTokens,
                document.OverlapPercent,
                document.PreferSentences,
                document.EmbeddingIndexes
                    .OrderBy(index => index.EmbeddingModel)
                    .Select(index => index.EmbeddingModel)
                    .ToList()))
            .ToListAsync();
    }

    private void LoadEmbeddingModels()
    {
        var models = _embeddingModels.AvailableModels.ToList();
        EnsureListed(models, EmbeddingModel);
        EnsureListed(models, BulkEmbeddingModel);
        EmbeddingModels = models;
    }

    private static void EnsureListed(List<EmbeddingModelOption> models, string modelId)
    {
        if (string.IsNullOrEmpty(modelId) ||
            models.Exists(model => string.Equals(model.Id, modelId, StringComparison.Ordinal)))
        {
            return;
        }

        models.Insert(0, new EmbeddingModelOption { Id = modelId, VectorSize = 0 });
    }

    private void RememberEmbeddingModel(string model)
    {
        if (string.IsNullOrEmpty(model))
            return;

        Response.Cookies.Append(EmbeddingModelCookieName, model, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(30),
            Path = "/"
        });
    }

    public sealed record DocumentListItem(
        int Id,
        string FileName,
        DateTime CreatedAt,
        int MaxTokens,
        int OverlapPercent,
        bool PreferSentences,
        IReadOnlyList<string> EmbeddingModels)
    {
        public ChunkOptions ChunkOptions => new(MaxTokens, OverlapPercent, PreferSentences);
    }
}
