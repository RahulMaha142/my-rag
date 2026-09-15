using RagNotes.Web.Data;
using RagNotes.Web.Services.Chunking;
using RagNotes.Web.Services.Embeddings;
using RagNotes.Web.Options;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<RagNotesDbContext>(options => 
    options.UseSqlite("Data Source=RagNotes.db"));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<ITextChunker, SimpleTextChunker>();
builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
