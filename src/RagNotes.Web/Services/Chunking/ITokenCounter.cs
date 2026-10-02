namespace RagNotes.Web.Services.Chunking;

public readonly record struct TextToken(int Start, int Length);

public interface ITokenCounter
{
  IReadOnlyList<TextToken> Tokenize(string text);
}
