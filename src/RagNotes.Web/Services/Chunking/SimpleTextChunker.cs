using System.Text.RegularExpressions;
using RagNotes.Web.Models;

namespace RagNotes.Web.Services.Chunking;

public class SimpleTextChunker : ITextChunker
{
  private const int ChunkSize = 1000;
  private const int Overlap = 200;

  private static readonly Regex SentenceSplit = new(
      @"(?<=[.!?])\s+",
      RegexOptions.Compiled);

  public IReadOnlyList<TextChunk> Chunk(string text, int documentId)
  {
    var chunks = new List<TextChunk>();
    if (string.IsNullOrWhiteSpace(text))
    {
      return chunks;
    }

    var sentences = SentenceSplit
        .Split(text.Trim())
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Select(s => s.Trim())
        .ToList();

    if (sentences.Count == 0)
    {
      return chunks;
    }

    var index = 0;
    var id = 1;
    var current = new List<string>();
    var currentLength = 0;

    void Flush()
    {
      if (current.Count == 0)
      {
        return;
      }

      chunks.Add(new TextChunk
      {
        Id = id++,
        DocumentId = documentId,
        Text = string.Join(" ", current),
        ChunkIndex = index++
      });
    }

    foreach (var sentence in sentences)
    {
      var separator = current.Count == 0 ? 0 : 1; // space between sentences
      var addedLength = sentence.Length + separator;

      if (current.Count > 0 && currentLength + addedLength > ChunkSize)
      {
        var overlap = TakeOverlap(current);
        Flush();
        current = overlap;
        currentLength = JoinLength(current);
        separator = current.Count == 0 ? 0 : 1;
        addedLength = sentence.Length + separator;
      }

      current.Add(sentence);
      currentLength += addedLength;
    }

    Flush();
    return chunks;
  }

  private static List<string> TakeOverlap(List<string> sentences)
  {
    var overlap = new List<string>();
    var length = 0;

    for (var i = sentences.Count - 1; i >= 0; i--)
    {
      var sentence = sentences[i];
      var separator = overlap.Count == 0 ? 0 : 1;
      var addedLength = sentence.Length + separator;
      if (length + addedLength > Overlap)
      {
        break;
      }

      overlap.Insert(0, sentence);
      length += addedLength;
    }

    return overlap;
  }

  private static int JoinLength(List<string> sentences)
  {
    if (sentences.Count == 0)
    {
      return 0;
    }

    return sentences.Sum(s => s.Length) + (sentences.Count - 1);
  }
}
