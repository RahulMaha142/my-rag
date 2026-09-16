using System.Text;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Web.Services.Llm;

public static class RagPromptBuilder
{
    public static string Build(string question, IReadOnlyList<SearchHit> chunks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a helpful assistant.");
        sb.AppendLine();
        sb.AppendLine("Answer the user's question using only the");
        sb.AppendLine("provided context.");
        sb.AppendLine();
        sb.AppendLine("If the context doesn't contain the answer,");
        sb.AppendLine("say that you don't know.");
        sb.AppendLine();
        sb.AppendLine("Context:");
        sb.AppendLine();

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            sb.AppendLine($"[{chunk.FileName} — Chunk {chunk.ChunkIndex}]");
            sb.AppendLine(chunk.Text);
            sb.AppendLine();
        }

        sb.AppendLine("Question:");
        sb.AppendLine();
        sb.Append(question);

        return sb.ToString();
    }
}
