namespace RagNotes.Web.Services.VectorStore;

public enum DocumentFilterMode
{
    Include,
    Exclude
}

public sealed record DocumentChunkFilter(
    DocumentFilterMode Mode,
    IReadOnlyList<int> DocumentIds)
{
    public const string IncludeValue = "include";
    public const string ExcludeValue = "exclude";

    public static DocumentChunkFilter? FromSelection(string? mode, IEnumerable<int>? selectedIds)
    {
        if (selectedIds is null)
            return null;

        var ids = selectedIds.Where(id => id > 0).Distinct().ToArray();
        if (ids.Length == 0)
            return null;

        var filterMode = string.Equals(mode, ExcludeValue, StringComparison.OrdinalIgnoreCase)
            ? DocumentFilterMode.Exclude
            : DocumentFilterMode.Include;

        return new DocumentChunkFilter(filterMode, ids);
    }
}
