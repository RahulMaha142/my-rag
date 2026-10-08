namespace RagNotes.Web.Services.Llm;

public abstract record LlmEvent;

public enum LlmTextKind
{
    Thinking,
    Answer
}

public sealed record LlmTextEvent(LlmTextKind Kind, string Text) : LlmEvent;

public sealed record LlmCompletedEvent(string? FinishReason) : LlmEvent;
