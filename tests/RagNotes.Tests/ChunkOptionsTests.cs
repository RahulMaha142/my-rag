using RagNotes.Web.Models;

namespace RagNotes.Tests;

public class ChunkOptionsTests
{
    [Fact]
    public void Overlap_tokens_are_a_clamped_percentage_of_the_cap()
    {
        var options = new ChunkOptions(MaxTokens: 256, OverlapPercent: 20, PreferSentences: true);

        Assert.Equal(51, options.OverlapTokens);
    }

    [Fact]
    public void Overlap_tokens_stay_below_the_cap()
    {
        var options = new ChunkOptions(MaxTokens: 32, OverlapPercent: 50, PreferSentences: false);

        Assert.Equal(16, options.OverlapTokens);
        Assert.True(options.OverlapTokens < options.MaxTokens);
    }

    [Theory]
    [InlineData(31, 20)]
    [InlineData(2049, 20)]
    [InlineData(256, -1)]
    [InlineData(256, 51)]
    public void Validate_rejects_out_of_range_values(int maxTokens, int overlapPercent)
    {
        Assert.NotNull(ChunkOptions.Validate(maxTokens, overlapPercent));
    }

    [Fact]
    public void Validate_accepts_the_defaults()
    {
        Assert.Null(ChunkOptions.Validate(
            ChunkOptions.DefaultMaxTokens,
            ChunkOptions.DefaultOverlapPercent));
    }
}
