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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RagNotesDbContext>(options =>
    options.UseSqlite("Data Source=RagNotes.db"));

builder.Services.AddRazorPages();
builder.Services.AddSingleton<ITextChunker, SimpleTextChunker>();
builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.Configure<QdrantOptions>(
    builder.Configuration.GetSection(QdrantOptions.SectionName));
builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});
builder.Services.AddHttpClient<ILlmService, OllamaLlmService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});
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
