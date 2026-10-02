using RagNotes.Web.Services.Chunking;

namespace RagNotes.Tests;

public class SimpleTextChunkerTests
{
    private readonly SimpleTextChunker _chunker = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t")]
    public void Blank_text_returns_no_chunks(string text)
    {
        var chunks = _chunker.Chunk(text, documentId: 1);

        Assert.Empty(chunks);
    }

    [Fact]
    public void Short_text_is_one_chunk()
    {
        var chunks = _chunker.Chunk("Hello world. This is a note.", documentId: 7);

        var chunk = Assert.Single(chunks);
        Assert.Equal(7, chunk.DocumentId);
        Assert.Equal(0, chunk.ChunkIndex);
        Assert.Equal("Hello world. This is a note.", chunk.Text);
    }

    [Fact]
    public void Long_text_splits_with_overlap_and_sequential_indexes()
    {
        var sentences = Enumerable.Range(1, 40)
            .Select(i => $"Sentence {i:D2} says something useful about notes.");
        var text = string.Join(" ", sentences);

        var chunks = _chunker.Chunk(text, documentId: 3);

        Assert.True(chunks.Count > 1);
        for (var i = 0; i < chunks.Count; i++)
        {
            Assert.Equal(3, chunks[i].DocumentId);
            Assert.Equal(i, chunks[i].ChunkIndex);
        }

        for (var i = 0; i < chunks.Count - 1; i++)
        {
            var lastSentence = chunks[i].Text.Split(". ")[^1];
            Assert.Contains(lastSentence, chunks[i + 1].Text);
        }
    }
}
