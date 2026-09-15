using RagNotes.Web.Data;
using RagNotes.Web.Services.Chunking;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<RagNotesDbContext>(options => 
    options.UseSqlite("Data Source=RagNotes.db"));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<ITextChunker, SimpleTextChunker>();

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
