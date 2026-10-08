using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
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
    public const string ThinkingCookieName = "rag-enable-thinking";

    private static readonly JsonSerializerOptions Ndjson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
    [Display(Name = "Enable thinking")]
    public bool EnableThinking { get; set; }

    [BindProperty]
    [Display(Name = "Document filter")]
    public string FilterMode { get; set; } = DocumentChunkFilter.IncludeValue;

    [BindProperty]
    public List<int> SelectedDocumentIds { get; set; } = [];

    public IReadOnlyList<string> ChatModels { get; private set; } = [];

    public IReadOnlyList<SearchableDocument> Documents { get; private set; } = [];

    public IReadOnlyList<SearchHit> Results { get; private set; } = [];

    public string? Answer { get; private set; }

    public string? Thinking { get; private set; }

    public string? AnswerModel { get; private set; }

    public string? FinishReason { get; private set; }

    public bool AnswerCutOff =>
        string.Equals(FinishReason, "length", StringComparison.OrdinalIgnoreCase);

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
        EnableThinking = string.Equals(
            Request.Cookies[ThinkingCookieName],
            "1",
            StringComparison.Ordinal);
        LoadChatModels();
        await LoadDocumentsAsync();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        HasSearched = true;
        PreparePreferences();
        await LoadDocumentsAsync(cancellationToken);

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Query))
            return Page();

        var question = Query.Trim();
        AppliedFilter = DocumentChunkFilter.FromSelection(FilterMode, SelectedDocumentIds);
        Results = await _search.SearchAsync(
            question,
            topK: 5,
            AppliedFilter,
            cancellationToken);

        if (Results.Count == 0)
            return Page();

        var thinking = new StringBuilder();
        var answer = new StringBuilder();
        var prompt = RagPromptBuilder.Build(question, Results);
        await foreach (var evt in _llm.StreamAsync(
            prompt,
            ChatModel,
            EnableThinking,
            cancellationToken))
        {
            switch (evt)
            {
                case LlmTextEvent text when text.Kind == LlmTextKind.Thinking:
                    thinking.Append(text.Text);
                    break;
                case LlmTextEvent text:
                    answer.Append(text.Text);
                    break;
                case LlmCompletedEvent completed:
                    FinishReason = completed.FinishReason;
                    break;
            }
        }

        if (thinking.Length > 0)
            Thinking = thinking.ToString();
        if (answer.Length > 0)
            Answer = answer.ToString();
        AnswerModel = ChatModel;
        return Page();
    }

    public async Task<IActionResult> OnPostStreamAsync(CancellationToken cancellationToken)
    {
        PreparePreferences();
        BeginNdjson();

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Query))
        {
            await WriteEventAsync(new
            {
                type = "error",
                text = ValidationMessage()
            }, cancellationToken);
            return new EmptyResult();
        }

        try
        {
            await WriteEventAsync(new
            {
                type = "status",
                text = "Searching notes…"
            }, cancellationToken);

            var question = Query.Trim();
            var filter = DocumentChunkFilter.FromSelection(FilterMode, SelectedDocumentIds);
            var results = await _search.SearchAsync(
                question,
                topK: 5,
                filter,
                cancellationToken);

            await WriteEventAsync(new
            {
                type = "sources",
                hits = results.Select(hit => new
                {
                    score = hit.Score,
                    fileName = hit.FileName,
                    chunkIndex = hit.ChunkIndex,
                    text = hit.Text
                })
            }, cancellationToken);

            if (results.Count == 0)
            {
                await WriteEventAsync(new
                {
                    type = "done",
                    finishReason = (string?)null
                }, cancellationToken);
                return new EmptyResult();
            }

            await WriteEventAsync(new
            {
                type = "status",
                text = "Generating…"
            }, cancellationToken);

            var prompt = RagPromptBuilder.Build(question, results);
            await foreach (var evt in _llm.StreamAsync(
                prompt,
                ChatModel,
                EnableThinking,
                cancellationToken))
            {
                switch (evt)
                {
                    case LlmTextEvent text when text.Kind == LlmTextKind.Thinking:
                        await WriteEventAsync(new
                        {
                            type = "thinking",
                            text = text.Text
                        }, cancellationToken);
                        break;
                    case LlmTextEvent text:
                        await WriteEventAsync(new
                        {
                            type = "token",
                            text = text.Text
                        }, cancellationToken);
                        break;
                    case LlmCompletedEvent completed:
                        await WriteEventAsync(new
                        {
                            type = "done",
                            finishReason = completed.FinishReason
                        }, cancellationToken);
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The browser aborted the request. Partial text is already on the page.
        }
        catch (Exception ex)
        {
            try
            {
                await WriteEventAsync(new
                {
                    type = "error",
                    text = ex.Message
                }, CancellationToken.None);
            }
            catch (Exception)
            {
                // The client may already have disconnected.
            }
        }

        return new EmptyResult();
    }

    private void PreparePreferences()
    {
        ChatModel = _omlx.ResolveChatModel(ChatModel);
        LoadChatModels();
        RememberChatModel(ChatModel);
        RememberThinking(EnableThinking);
    }

    private void BeginNdjson()
    {
        Response.ContentType = "application/x-ndjson; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
    }

    private string ValidationMessage()
    {
        var message = ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(error => !string.IsNullOrWhiteSpace(error));
        return string.IsNullOrWhiteSpace(message)
            ? "Enter a search query."
            : message;
    }

    private async Task WriteEventAsync(object payload, CancellationToken cancellationToken)
    {
        await Response.WriteAsync(JsonSerializer.Serialize(payload, Ndjson), cancellationToken);
        await Response.WriteAsync("\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
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

        Response.Cookies.Append(ChatModelCookieName, model, PreferenceCookie());
    }

    private void RememberThinking(bool enabled)
    {
        Response.Cookies.Append(
            ThinkingCookieName,
            enabled ? "1" : "0",
            PreferenceCookie());
    }

    private static CookieOptions PreferenceCookie() => new()
    {
        HttpOnly = true,
        IsEssential = true,
        SameSite = SameSiteMode.Lax,
        MaxAge = TimeSpan.FromDays(30),
        Path = "/Search"
    };

    public sealed record SearchableDocument(int Id, string FileName, DateTime CreatedAt);
}
