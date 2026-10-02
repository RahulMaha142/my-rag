using RagNotes.Web.Models;

namespace RagNotes.Web.Services.Chunking;

public interface ITextChunker
{
  IReadOnlyList<TextChunk> Chunk(string text, int documentId, ChunkOptions options);
}
