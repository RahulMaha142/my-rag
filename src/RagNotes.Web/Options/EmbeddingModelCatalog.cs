namespace RagNotes.Web.Options;

/// <summary>
/// Shared helpers for resolving embedding models from a default id + catalog list.
/// </summary>
public static class EmbeddingModelCatalog
{
    public static IReadOnlyList<EmbeddingModelOption> Available(
        string? defaultModel,
        IEnumerable<EmbeddingModelOption>? models)
    {
        var result = new List<EmbeddingModelOption>();
        foreach (var model in models ?? [])
            Add(result, model);

        if (!string.IsNullOrWhiteSpace(defaultModel) &&
            Find(result, defaultModel) is null)
        {
            result.Insert(0, new EmbeddingModelOption
            {
                Id = defaultModel.Trim(),
                VectorSize = 0
            });
        }

        return result;
    }

    public static string Resolve(
        string? requested,
        string? defaultModel,
        IReadOnlyList<EmbeddingModelOption> available)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            var match = Find(available, requested);
            if (match is not null)
                return match.Id;
        }

        if (!string.IsNullOrWhiteSpace(defaultModel))
        {
            var configured = defaultModel.Trim();
            return Find(available, configured)?.Id ?? configured;
        }

        return available.FirstOrDefault()?.Id ?? string.Empty;
    }

    public static ulong GetVectorSize(
        string modelId,
        IReadOnlyList<EmbeddingModelOption> available,
        ulong fallback = 0)
    {
        var match = Find(available, modelId);
        if (match is not null && match.VectorSize > 0)
            return match.VectorSize;

        return fallback;
    }

    public static EmbeddingModelOption? Find(
        IReadOnlyList<EmbeddingModelOption> models,
        string candidate)
    {
        var trimmed = candidate.Trim();
        foreach (var model in models)
        {
            if (string.Equals(model.Id, trimmed, StringComparison.Ordinal))
                return model;
        }

        return null;
    }

    private static void Add(List<EmbeddingModelOption> models, EmbeddingModelOption? model)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Id))
            return;

        var trimmed = model.Id.Trim();
        if (Find(models, trimmed) is not null)
            return;

        models.Add(new EmbeddingModelOption
        {
            Id = trimmed,
            VectorSize = model.VectorSize
        });
    }
}
