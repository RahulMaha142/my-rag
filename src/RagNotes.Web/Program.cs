using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using RagNotes.Web.Data;
using RagNotes.Web.Options;
using RagNotes.Web.Services;
using RagNotes.Web.Services.Chunking;
using RagNotes.Web.Services.Embeddings;
using RagNotes.Web.Services.Llm;
using RagNotes.Web.Services.VectorStore;

LoadEnvFile();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RagNotesDbContext>(options =>
    options.UseSqlite("Data Source=RagNotes.db"));

builder.Services.AddRazorPages();
builder.Services.AddSingleton<ITextChunker, SimpleTextChunker>();
builder.Services.Configure<InferenceOptions>(
    builder.Configuration.GetSection(InferenceOptions.SectionName));
builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.Configure<OmlxOptions>(
    builder.Configuration.GetSection(OmlxOptions.SectionName));
builder.Services.Configure<QdrantOptions>(
    builder.Configuration.GetSection(QdrantOptions.SectionName));

var provider = builder.Configuration[$"{InferenceOptions.SectionName}:Provider"] ?? "Omlx";
switch (provider.Trim().ToLowerInvariant())
{
    case "ollama":
        builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
        });
        builder.Services.AddHttpClient<ILlmService, OllamaLlmService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
        });
        break;
    case "omlx":
        builder.Services.AddHttpClient<IEmbeddingService, OmlxEmbeddingService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OmlxOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
            ApplyApiKey(client, options.ApiKey);
        });
        builder.Services.AddHttpClient<ILlmService, OmlxLlmService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OmlxOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
            ApplyApiKey(client, options.ApiKey);
        });
        break;
    default:
        throw new InvalidOperationException(
            $"Unknown Inference:Provider '{provider}'. Use 'Ollama' or 'Omlx'.");
}
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<QdrantOptions>>().Value;
    return new QdrantClient(options.Host, options.Port);
});
builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();
builder.Services.AddScoped<DocumentIndexingService>();
builder.Services.AddScoped<SemanticSearchService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
    await vectorStore.EnsureCollectionAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();

static void LoadEnvFile()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            var envPath = Path.Combine(dir.FullName, ".env");
            if (File.Exists(envPath))
            {
                ApplyEnvFile(envPath);
                return;
            }

            dir = dir.Parent;
        }
    }
}

static void ApplyEnvFile(string path)
{
    foreach (var rawLine in File.ReadAllLines(path))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#'))
            continue;

        if (line.StartsWith("export ", StringComparison.Ordinal))
            line = line["export ".Length..].Trim();

        var separator = line.IndexOf('=');
        if (separator <= 0)
            continue;

        var key = line[..separator].Trim();
        var value = line[(separator + 1)..].Trim().Trim('"').Trim('\'');
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            Environment.SetEnvironmentVariable(key, value);
    }

    var apiKey = Environment.GetEnvironmentVariable("Omlx_api_key");
    if (!string.IsNullOrWhiteSpace(apiKey) &&
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Omlx__ApiKey")))
    {
        Environment.SetEnvironmentVariable("Omlx__ApiKey", apiKey);
    }
}

static void ApplyApiKey(HttpClient client, string? apiKey)
{
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }
}
