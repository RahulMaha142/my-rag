using System.Text.RegularExpressions;
using RagNotes.Web.Models;

namespace RagNotes.Web.Services.Chunking;

public class SimpleTextChunker : ITextChunker
{
  private static readonly Regex SentenceBoundary = new(
      @"(?<=[.!?])\s+",
      RegexOptions.Compiled);

  private readonly ITokenCounter _tokenCounter;

  public SimpleTextChunker(ITokenCounter tokenCounter)
  {
    _tokenCounter = tokenCounter;
  }

  public IReadOnlyList<TextChunk> Chunk(string text, int documentId, ChunkOptions options)
  {
    var chunks = new List<TextChunk>();
    if (string.IsNullOrWhiteSpace(text))
    {
      return chunks;
    }

    var tokens = _tokenCounter.Tokenize(text);
    if (tokens.Count == 0)
    {
      return chunks;
    }

    var index = 0;
    var id = 1;

    void Emit(int tokenStart, int tokenEnd)
    {
      if (tokenEnd <= tokenStart)
      {
        return;
      }

      var startChar = tokens[tokenStart].Start;
      var last = tokens[tokenEnd - 1];
      var endChar = last.Start + last.Length;
      var slice = text[startChar..endChar].Trim();
      if (slice.Length == 0)
      {
        return;
      }

      chunks.Add(new TextChunk
      {
        Id = id++,
        DocumentId = documentId,
        Text = slice,
        ChunkIndex = index++
      });
    }

    void EmitWindows(int tokenStart, int tokenEnd)
    {
      var overlap = options.OverlapTokens;
      var start = tokenStart;
      while (start < tokenEnd)
      {
        var end = Math.Min(start + options.MaxTokens, tokenEnd);
        Emit(start, end);
        if (end >= tokenEnd)
        {
          break;
        }

        var next = end - overlap;
        if (next <= start)
        {
          next = start + 1;
        }

        start = next;
      }
    }

    if (!options.PreferSentences)
    {
      EmitWindows(0, tokens.Count);
      return chunks;
    }

    var sentences = GroupSentences(text, tokens);
    var buffer = new List<SentencePiece>();
    var bufferTokens = 0;

    void EmitBuffer()
    {
      if (buffer.Count == 0)
      {
        return;
      }

      Emit(buffer[0].TokenStart, buffer[^1].TokenEnd);
    }

    foreach (var sentence in sentences)
    {
      var size = sentence.TokenCount;
      if (size > options.MaxTokens)
      {
        EmitBuffer();
        buffer.Clear();
        bufferTokens = 0;
        EmitWindows(sentence.TokenStart, sentence.TokenEnd);
        continue;
      }

      if (buffer.Count > 0 && bufferTokens + size > options.MaxTokens)
      {
        var emitted = buffer.ToList();
        EmitBuffer();
        buffer = TakeSentenceOverlap(emitted, options.OverlapTokens);
        bufferTokens = buffer.Sum(piece => piece.TokenCount);
        while (buffer.Count > 0 && bufferTokens + size > options.MaxTokens)
        {
          bufferTokens -= buffer[0].TokenCount;
          buffer.RemoveAt(0);
        }
      }

      buffer.Add(sentence);
      bufferTokens += size;
    }

    EmitBuffer();
    return chunks;
  }

  private static List<SentencePiece> TakeSentenceOverlap(
      List<SentencePiece> sentences,
      int overlapTokens)
  {
    var overlap = new List<SentencePiece>();
    var count = 0;
    for (var i = sentences.Count - 1; i >= 0; i--)
    {
      var size = sentences[i].TokenCount;
      if (count + size > overlapTokens)
      {
        break;
      }

      overlap.Insert(0, sentences[i]);
      count += size;
    }

    return overlap;
  }

  private static List<SentencePiece> GroupSentences(string text, IReadOnlyList<TextToken> tokens)
  {
    var sentences = new List<SentencePiece>();
    var tokenIndex = 0;

    foreach (var (_, end) in SentenceRanges(text))
    {
      var tokenStart = tokenIndex;
      while (tokenIndex < tokens.Count && tokens[tokenIndex].Start < end)
      {
        tokenIndex++;
      }

      if (tokenIndex > tokenStart)
      {
        sentences.Add(new SentencePiece(tokenStart, tokenIndex));
      }
    }

    return sentences;
  }

  private static List<(int Start, int End)> SentenceRanges(string text)
  {
    var ranges = new List<(int Start, int End)>();
    var start = 0;
    foreach (Match match in SentenceBoundary.Matches(text))
    {
      var end = match.Index + match.Length;
      if (end > start)
      {
        ranges.Add((start, end));
      }

      start = end;
    }

    if (start < text.Length)
    {
      ranges.Add((start, text.Length));
    }

    return ranges;
  }

  private readonly record struct SentencePiece(int TokenStart, int TokenEnd)
  {
    public int TokenCount => TokenEnd - TokenStart;
  }
}
