namespace RagNotes.Web.Options;

public class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6334;

    /// <summary>
    /// Collection name prefix. Per-model collections are named
    /// <c>{CollectionName}__{sanitized-model-id}</c>. The unsuffixed name is
    /// still used for the default embedding model when that legacy collection
    /// already exists.
    /// </summary>
    public string CollectionName { get; set; } = "rag-notes";
}
