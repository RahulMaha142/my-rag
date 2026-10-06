namespace RagNotes.Web.Options;

public class OmlxOptions
{
    public const string SectionName = "Omlx";

    public string BaseUrl { get; set; } = "http://127.0.0.1:8000";
    public string EmbeddingModel { get; set; } = "bge-m3-mlx-4bit";
    public string ChatModel { get; set; } = "Qwen3.8-27B-4bit";
    public string[] ChatModels { get; set; } =
    [
        "Qwen3.8-27B-4bit",
        "Qwen3.8-27B-8bit",
        "granite-3.3-8b-instruct-6bit"
    ];
    public string? ApiKey { get; set; }

    public IReadOnlyList<string> AvailableChatModels
    {
        get
        {
            var models = new List<string>();
            foreach (var model in ChatModels ?? [])
                AddModel(models, model);

            if (models.Count == 0)
                AddModel(models, ChatModel);

            return models;
        }
    }

    public string ResolveChatModel(string? requested)
    {
        var available = AvailableChatModels;
        if (!string.IsNullOrWhiteSpace(requested))
        {
            var match = Find(available, requested);
            if (match is not null)
                return match;
        }

        if (!string.IsNullOrWhiteSpace(ChatModel))
        {
            var configured = ChatModel.Trim();
            return Find(available, configured) ?? configured;
        }

        return available.FirstOrDefault() ?? string.Empty;
    }

    private static string? Find(IReadOnlyList<string> models, string candidate)
    {
        var trimmed = candidate.Trim();
        foreach (var model in models)
        {
            if (string.Equals(model, trimmed, StringComparison.Ordinal))
                return model;
        }

        return null;
    }

    private static void AddModel(List<string> models, string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return;

        var trimmed = model.Trim();
        if (Find(models, trimmed) is not null)
            return;

        models.Add(trimmed);
    }
}
