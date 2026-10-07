using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Tests;

public class DocumentChunkFilterTests
{
    [Fact]
    public void Empty_selection_searches_all_chunks()
    {
        Assert.Null(DocumentChunkFilter.FromSelection("include", []));
        Assert.Null(DocumentChunkFilter.FromSelection("exclude", []));
        Assert.Null(DocumentChunkFilter.FromSelection("include", null));
    }

    [Fact]
    public void Include_keeps_only_the_chosen_documents()
    {
        var filter = DocumentChunkFilter.FromSelection("include", [3, 3, 1]);

        Assert.NotNull(filter);
        Assert.Equal(DocumentFilterMode.Include, filter.Mode);
        Assert.Equal([3, 1], filter.DocumentIds);
    }

    [Fact]
    public void Exclude_drops_the_chosen_documents()
    {
        var filter = DocumentChunkFilter.FromSelection("Exclude", [4, 9]);

        Assert.NotNull(filter);
        Assert.Equal(DocumentFilterMode.Exclude, filter.Mode);
        Assert.Equal([4, 9], filter.DocumentIds);
    }

    [Fact]
    public void Unknown_mode_with_a_selection_includes_those_documents()
    {
        var filter = DocumentChunkFilter.FromSelection("something-else", [2]);

        Assert.NotNull(filter);
        Assert.Equal(DocumentFilterMode.Include, filter.Mode);
        Assert.Equal([2], filter.DocumentIds);
    }
}
