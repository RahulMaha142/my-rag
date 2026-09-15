namespace RagNotes.Web.Options;

public class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6334;
    public string CollectionName { get; set; } = "rag-notes";
    public ulong VectorSize { get; set; } = 768;
}
