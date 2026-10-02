using RagNotes.Web.Services.VectorStore;

namespace RagNotes.Tests;

public class QdrantPointIdTests
{
    [Fact]
    public void CreatePointId_is_stable_for_the_same_inputs()
    {
        var first = QdrantVectorStore.CreatePointId(documentId: 4, chunkIndex: 2);
        var second = QdrantVectorStore.CreatePointId(documentId: 4, chunkIndex: 2);

        Assert.Equal(first, second);
    }

    [Fact]
    public void CreatePointId_changes_when_either_input_changes()
    {
        var original = QdrantVectorStore.CreatePointId(documentId: 4, chunkIndex: 2);

        Assert.NotEqual(original, QdrantVectorStore.CreatePointId(documentId: 4, chunkIndex: 3));
        Assert.NotEqual(original, QdrantVectorStore.CreatePointId(documentId: 5, chunkIndex: 2));
    }
}
