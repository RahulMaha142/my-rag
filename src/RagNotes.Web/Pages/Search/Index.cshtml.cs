using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RagNotes.Web.Data;
using RagNotes.Web.Options;
using RagNotes.Web.Services;
using RagNotes.Web.Services.Llm;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Web.Pages.Search;

public class IndexModel : PageModel
{
    public const string ChatModelCookieName = "rag-chat-model";

    private readonly SemanticSearchService _search;
    private readonly ILlmService _llm;
    private readonly RagNotesDbContext _db;
    private readonly OmlxOptions _omlx;

    public IndexModel(
        SemanticSearchService search,
        ILlmService llm,
        RagNotesDbContext db,
        IOptions<OmlxOptions> omlx)
    {
        _search = search;
        _llm = llm;
        _db = db;
        _omlx = omlx.Value;
    }

    [BindProperty]
    [Required(ErrorMessage = "Enter a search query.")]
    [Display(Name = "Query")]
    public string Query { get; set; } = string.Empty;

    [BindProperty]
    [Display(Name = "Model")]
    public string ChatModel { get; set; } = string.Empty;

    [BindProperty]
    [Display(Name = "Document filter")]
    public string FilterMode { get; set; } = DocumentChunkFilter.IncludeValue;

    [BindProperty]
    public List<int> SelectedDocumentIds { get; set; } = [];

    public IReadOnlyList<string> ChatModels { get; private set; } = [];

    public IReadOnlyList<SearchableDocument> Documents { get; private set; } = [];

    public IReadOnlyList<SearchHit> Results { get; private set; } = [];

    public string? Answer { get; private set; }

    public string? AnswerModel { get; private set; }

    public bool HasSearched { get; private set; }

    public DocumentChunkFilter? AppliedFilter { get; private set; }

    public string AppliedFilterSummary
    {
        get
        {
            if (AppliedFilter is null || AppliedFilter.DocumentIds.Count == 0)
                return "All documents";

            var names = AppliedFilter.DocumentIds
                .Select(id => Documents.FirstOrDefault(document => document.Id == id)?.FileName
                    ?? $"document {id}");
            var list = string.Join(", ", names);
            return AppliedFilter.Mode == DocumentFilterMode.Exclude
                ? $"Excluding {list}"
                : $"Only {list}";
        }
    }

    public async Task OnGetAsync()
    {
        ChatModel = _omlx.ResolveChatModel(Request.Cookies[ChatModelCookieName]);
        LoadChatModels();
        await LoadDocumentsAsync();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        HasSearched = true;
        ChatModel = _omlx.ResolveChatModel(ChatModel);
        LoadChatModels();
        RememberChatModel(ChatModel);
        await LoadDocumentsAsync(cancellationToken);

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Query))
        {
            return Page();
        }

        var question = Query.Trim();
        AppliedFilter = DocumentChunkFilter.FromSelection(FilterMode, SelectedDocumentIds);
        Results = await _search.SearchAsync(
            question,
            topK: 5,
            AppliedFilter,
            cancellationToken);

        if (Results.Count > 0)
        {
            var prompt = RagPromptBuilder.Build(question, Results);
            Answer = await _llm.GenerateAsync(prompt, ChatModel, cancellationToken);
            AnswerModel = ChatModel;
        }

        return Page();
    }

    private async Task LoadDocumentsAsync(CancellationToken cancellationToken = default)
    {
        Documents = await _db.Documents
            .OrderBy(document => document.FileName)
            .ThenBy(document => document.CreatedAt)
            .Select(document => new SearchableDocument(
                document.Id,
                document.FileName,
                document.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private void LoadChatModels()
    {
        var models = _omlx.AvailableChatModels.ToList();
        if (!string.IsNullOrEmpty(ChatModel) &&
            !models.Exists(model => string.Equals(model, ChatModel, StringComparison.Ordinal)))
        {
            models.Insert(0, ChatModel);
        }

        ChatModels = models;
    }

    private void RememberChatModel(string model)
    {
        if (string.IsNullOrEmpty(model))
            return;

        Response.Cookies.Append(ChatModelCookieName, model, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(30),
            Path = "/Search"
        });
    }

    public sealed record SearchableDocument(int Id, string FileName, DateTime CreatedAt);
}
