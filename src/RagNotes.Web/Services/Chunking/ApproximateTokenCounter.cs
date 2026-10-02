namespace RagNotes.Web.Services.Chunking;

public class ApproximateTokenCounter : ITokenCounter
{
  private const int LongWordLength = 16;
  private const int LongWordPieceLength = 4;

  public IReadOnlyList<TextToken> Tokenize(string text)
  {
    var tokens = new List<TextToken>();
    if (string.IsNullOrEmpty(text))
    {
      return tokens;
    }

    var index = 0;
    while (index < text.Length)
    {
      if (char.IsWhiteSpace(text[index]))
      {
        index++;
        continue;
      }

      if (char.IsLetterOrDigit(text[index]))
      {
        var start = index;
        while (index < text.Length && char.IsLetterOrDigit(text[index]))
        {
          index++;
        }

        AddWord(tokens, start, index - start);
        continue;
      }

      tokens.Add(new TextToken(index, 1));
      index++;
    }

    return tokens;
  }

  private static void AddWord(List<TextToken> tokens, int start, int length)
  {
    if (length <= LongWordLength)
    {
      tokens.Add(new TextToken(start, length));
      return;
    }

    var offset = 0;
    while (offset < length)
    {
      var piece = Math.Min(LongWordPieceLength, length - offset);
      tokens.Add(new TextToken(start + offset, piece));
      offset += piece;
    }
  }
}
