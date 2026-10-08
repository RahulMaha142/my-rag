using RagNotes.Web.Services.Llm;

namespace RagNotes.Tests;

public class OmlxChatStreamParserTests
{
    [Fact]
    public void Reasoning_and_content_stay_separate()
    {
        var sse = string.Join('\n',
        [
            """data: {"choices":[{"delta":{"reasoning_content":"Think "}}]}""",
            "",
            """data: {"choices":[{"delta":{"reasoning_content":"more","content":"Answer"}}]}""",
            "",
            """data: {"choices":[{"delta":{},"finish_reason":"stop"}]}""",
            "",
            "data: [DONE]"
        ]);

        var events = OmlxChatStreamParser.Parse(sse);

        Assert.Collection(
            events,
            evt => AssertText(evt, LlmTextKind.Thinking, "Think "),
            evt => AssertText(evt, LlmTextKind.Thinking, "more"),
            evt => AssertText(evt, LlmTextKind.Answer, "Answer"),
            evt => AssertCompleted(evt, "stop"));
    }

    [Fact]
    public void Done_marker_ends_the_stream()
    {
        var sse = string.Join('\n',
        [
            """data: {"choices":[{"delta":{"content":"Hi"}}]}""",
            "",
            "data: [DONE]",
            """data: {"choices":[{"delta":{"content":" hidden"}}]}"""
        ]);

        var events = OmlxChatStreamParser.Parse(sse);

        Assert.Collection(
            events,
            evt => AssertText(evt, LlmTextKind.Answer, "Hi"),
            evt => AssertCompleted(evt, null));
    }

    [Fact]
    public void Length_finish_reason_is_returned_and_stops_the_stream()
    {
        var sse = string.Join('\n',
        [
            """data: {"choices":[{"delta":{"reasoning_content":"long"}}]}""",
            "",
            """data: {"choices":[{"delta":{},"finish_reason":"length"}]}""",
            """data: {"choices":[{"delta":{"content":"nope"}}]}"""
        ]);

        var events = OmlxChatStreamParser.Parse(sse);

        Assert.Collection(
            events,
            evt => AssertText(evt, LlmTextKind.Thinking, "long"),
            evt => AssertCompleted(evt, "length"));
    }

    private static void AssertText(LlmEvent evt, LlmTextKind kind, string text)
    {
        var textEvent = Assert.IsType<LlmTextEvent>(evt);
        Assert.Equal(kind, textEvent.Kind);
        Assert.Equal(text, textEvent.Text);
    }

    private static void AssertCompleted(LlmEvent evt, string? finishReason)
    {
        var completed = Assert.IsType<LlmCompletedEvent>(evt);
        Assert.Equal(finishReason, completed.FinishReason);
    }
}
