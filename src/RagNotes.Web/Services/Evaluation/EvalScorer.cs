using System.Text;
using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Web.Services.Evaluation;

public static class EvalScorer
{
    public static string CollapseWhitespace(string text)
    {
        var collapsed = new StringBuilder(text.Length);
        var pendingSpace = false;
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = collapsed.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                collapsed.Append(' ');
                pendingSpace = false;
            }

            collapsed.Append(ch);
        }

        return collapsed.ToString();
    }

    public static bool ContainsQuote(string text, string quote)
    {
        var needle = CollapseWhitespace(quote);
        if (needle.Length == 0)
            return false;

        return CollapseWhitespace(text).Contains(needle, StringComparison.Ordinal);
    }

    public static bool Matches(EvalQuestion question, IReadOnlyList<SearchHit> hits)
    {
        if (string.IsNullOrWhiteSpace(question.MustContain))
            return false;

        foreach (var hit in hits)
        {
            if (!question.ExpectedFiles.Exists(file =>
                    string.Equals(file, hit.FileName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (ContainsQuote(hit.Text, question.MustContain))
                return true;
        }

        return false;
    }
}
