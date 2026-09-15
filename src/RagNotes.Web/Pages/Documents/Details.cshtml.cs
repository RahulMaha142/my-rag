using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagNotes.Web.Data;
using RagNotes.Web.Models;
using RagNotes.Web.Services.Chunking;
using RagNotes.Web.Services.Embeddings;

namespace RagNotes.Web.Pages.Documents;

public class DetailsModel : PageModel
{
    private readonly RagNotesDbContext _db;
    private readonly ITextChunker _chunker;
    private readonly IEmbeddingService _embeddings;

    public DetailsModel(
        RagNotesDbContext db,
        ITextChunker chunker,
        IEmbeddingService embeddings)
    {
        _db = db;
        _chunker = chunker;
        _embeddings = embeddings;
    }

    public Document Document { get; private set; } = null!;
    public IReadOnlyList<ChunkEmbeddingItem> Chunks { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var document = await _db.Documents.FindAsync([id], cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        Document = document;

        var textChunks = _chunker.Chunk(document.Content, document.Id);
        var items = new List<ChunkEmbeddingItem>(textChunks.Count);

        foreach (var chunk in textChunks)
        {
            var embedding = await _embeddings.EmbedAsync(chunk.Text, cancellationToken);
            items.Add(new ChunkEmbeddingItem(chunk, embedding));
        }

        Chunks = items;
        return Page();
    }

    public sealed record ChunkEmbeddingItem(TextChunk Chunk, float[] Embedding);
}
