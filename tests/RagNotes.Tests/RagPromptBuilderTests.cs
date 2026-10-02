using RagNotes.Web.Services.Llm;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Tests;

public class RagPromptBuilderTests
{
    [Fact]
    public void Prompt_includes_question_and_chunks_in_order()
    {
        var hits = new SearchHit[]
        {
            new(0.9f, 1, "a.md", 0, "Alpha text"),
            new(0.5f, 2, "b.txt", 3, "Beta text")
        };

        var prompt = RagPromptBuilder.Build("What is alpha?", hits);

        Assert.Contains("using only the", prompt);
        Assert.Contains("say that you don't know.", prompt);
        Assert.Contains("[a.md — Chunk 0]", prompt);
        Assert.Contains("Alpha text", prompt);
        Assert.Contains("[b.txt — Chunk 3]", prompt);
        Assert.Contains("Beta text", prompt);
        Assert.Contains("What is alpha?", prompt);
        Assert.True(prompt.IndexOf("Alpha text", StringComparison.Ordinal) <
                    prompt.IndexOf("Beta text", StringComparison.Ordinal));
        Assert.True(prompt.IndexOf("Beta text", StringComparison.Ordinal) <
                    prompt.IndexOf("Question:", StringComparison.Ordinal));
    }

    [Fact]
    public void Empty_chunks_still_include_instructions_and_question()
    {
        var prompt = RagPromptBuilder.Build("Why?", []);

        Assert.Contains("You are a helpful assistant.", prompt);
        Assert.Contains("say that you don't know.", prompt);
        Assert.Contains("Question:", prompt);
        Assert.Contains("Why?", prompt);
        Assert.DoesNotContain("Chunk", prompt);
    }
}
