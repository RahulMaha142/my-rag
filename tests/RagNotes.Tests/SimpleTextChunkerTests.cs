using RagNotes.Web.Models;
using RagNotes.Web.Services.Chunking;

namespace RagNotes.Tests;

public class SimpleTextChunkerTests
{
    private readonly ApproximateTokenCounter _counter = new();
    private readonly SimpleTextChunker _chunker;

    public SimpleTextChunkerTests()
    {
        _chunker = new SimpleTextChunker(_counter);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t")]
    public void Blank_text_returns_no_chunks(string text)
    {
        var chunks = _chunker.Chunk(text, documentId: 1, ChunkOptions.Default);

        Assert.Empty(chunks);
    }

    [Fact]
    public void Short_text_is_one_chunk()
    {
        var chunks = _chunker.Chunk("Hello world. This is a note.", documentId: 7, ChunkOptions.Default);

        var chunk = Assert.Single(chunks);
        Assert.Equal(7, chunk.DocumentId);
        Assert.Equal(0, chunk.ChunkIndex);
        Assert.Equal("Hello world. This is a note.", chunk.Text);
    }

    [Fact]
    public void Sentence_chunks_stay_under_the_cap_and_end_on_a_boundary()
    {
        var sentences = Enumerable.Range(1, 12)
            .Select(i => $"Sentence {i:D2} says something useful.");
        var text = string.Join(" ", sentences);
        var options = new ChunkOptions(MaxTokens: 16, OverlapPercent: 0, PreferSentences: true);

        var chunks = _chunker.Chunk(text, documentId: 3, options);

        Assert.True(chunks.Count > 1);
        for (var i = 0; i < chunks.Count; i++)
        {
            Assert.Equal(3, chunks[i].DocumentId);
            Assert.Equal(i, chunks[i].ChunkIndex);
            Assert.True(_counter.Tokenize(chunks[i].Text).Count <= options.MaxTokens);
            Assert.EndsWith(".", chunks[i].Text);
        }
    }

    [Fact]
    public void Oversized_sentence_is_cut_and_still_respects_the_cap()
    {
        var words = Enumerable.Range(1, 30).Select(i => $"word{i}");
        var text = string.Join(" ", words) + ".";
        var options = new ChunkOptions(MaxTokens: 8, OverlapPercent: 0, PreferSentences: true);

        var chunks = _chunker.Chunk(text, documentId: 4, options);

        Assert.True(chunks.Count > 1);
        Assert.Contains(chunks, chunk => !chunk.Text.EndsWith('.'));
        Assert.All(chunks, chunk =>
            Assert.True(_counter.Tokenize(chunk.Text).Count <= options.MaxTokens));
    }

    [Fact]
    public void Token_windows_can_end_mid_sentence()
    {
        var text = "one two three four five six seven eight nine ten.";
        var options = new ChunkOptions(MaxTokens: 4, OverlapPercent: 0, PreferSentences: false);

        var chunks = _chunker.Chunk(text, documentId: 5, options);

        Assert.Equal("one two three four", chunks[0].Text);
        Assert.DoesNotContain(".", chunks[0].Text);
    }

    [Fact]
    public void Overlap_percent_repeats_the_tail_of_the_previous_chunk()
    {
        var words = Enumerable.Range(0, 20).Select(i => $"w{i}");
        var text = string.Join(" ", words);
        var options = new ChunkOptions(MaxTokens: 10, OverlapPercent: 50, PreferSentences: false);

        var chunks = _chunker.Chunk(text, documentId: 6, options);

        Assert.StartsWith("w0 ", chunks[0].Text);
        Assert.EndsWith(" w9", chunks[0].Text);
        Assert.StartsWith("w5 ", chunks[1].Text);
        Assert.Contains("w5", chunks[0].Text);
    }

    [Fact]
    public void Zero_overlap_does_not_share_tokens()
    {
        var words = Enumerable.Range(0, 20).Select(i => $"w{i}");
        var text = string.Join(" ", words);
        var options = new ChunkOptions(MaxTokens: 10, OverlapPercent: 0, PreferSentences: false);

        var chunks = _chunker.Chunk(text, documentId: 8, options);

        Assert.Equal("w0 w1 w2 w3 w4 w5 w6 w7 w8 w9", chunks[0].Text);
        Assert.Equal("w10 w11 w12 w13 w14 w15 w16 w17 w18 w19", chunks[1].Text);
    }

    [Fact]
    public void Sentence_overlap_keeps_whole_sentences_and_the_cap()
    {
        var sentences = Enumerable.Range(0, 4).Select(i => $"s{i} aa bb cc.");
        var text = string.Join(" ", sentences);
        var options = new ChunkOptions(MaxTokens: 10, OverlapPercent: 50, PreferSentences: true);

        var chunks = _chunker.Chunk(text, documentId: 9, options);

        Assert.Equal("s0 aa bb cc. s1 aa bb cc.", chunks[0].Text);
        Assert.StartsWith("s1 aa bb cc.", chunks[1].Text);
        Assert.All(chunks, chunk =>
            Assert.True(_counter.Tokenize(chunk.Text).Count <= options.MaxTokens));
    }

    [Fact]
    public void Overlap_sentences_are_dropped_when_they_would_break_the_cap()
    {
        var text = "aa bb cc. dd ee ff. gg hh ii jj kk ll.";
        var options = new ChunkOptions(MaxTokens: 10, OverlapPercent: 50, PreferSentences: true);

        var chunks = _chunker.Chunk(text, documentId: 10, options);

        Assert.Equal("aa bb cc. dd ee ff.", chunks[0].Text);
        Assert.Equal("gg hh ii jj kk ll.", chunks[1].Text);
        Assert.All(chunks, chunk =>
            Assert.True(_counter.Tokenize(chunk.Text).Count <= options.MaxTokens));
    }

    [Fact]
    public void Sentences_before_an_oversized_sentence_are_kept()
    {
        var longSentence = string.Join(" ", Enumerable.Range(1, 20).Select(i => $"word{i}")) + ".";
        var text = "Short note. " + longSentence;
        var options = new ChunkOptions(MaxTokens: 8, OverlapPercent: 0, PreferSentences: true);

        var chunks = _chunker.Chunk(text, documentId: 12, options);

        Assert.Equal("Short note.", chunks[0].Text);
        Assert.All(chunks, chunk =>
            Assert.True(_counter.Tokenize(chunk.Text).Count <= options.MaxTokens));
    }

    [Fact]
    public void High_overlap_still_advances_and_finishes()
    {
        var sentences = Enumerable.Range(0, 40).Select(i => $"s{i} aa bb cc.");
        var text = string.Join(" ", sentences);
        var options = new ChunkOptions(MaxTokens: 32, OverlapPercent: 50, PreferSentences: true);

        var chunks = _chunker.Chunk(text, documentId: 11, options);

        Assert.True(chunks.Count > 1);
        Assert.True(chunks.Count < 40);
        var starts = chunks.Select(chunk => int.Parse(chunk.Text.Split(' ')[0][1..])).ToList();
        for (var i = 1; i < starts.Count; i++)
        {
            Assert.True(starts[i] > starts[i - 1]);
        }

        Assert.All(chunks, chunk =>
            Assert.True(_counter.Tokenize(chunk.Text).Count <= options.MaxTokens));
    }
}
