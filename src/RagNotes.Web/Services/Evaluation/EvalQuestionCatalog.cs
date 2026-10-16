using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace RagNotes.Web.Services.Evaluation;

public sealed class EvalQuestionCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _path;
    private readonly Lazy<IReadOnlyList<EvalQuestion>> _questions;

    public EvalQuestionCatalog(IWebHostEnvironment environment)
    {
        _path = ResolvePath(environment.ContentRootPath);
        _questions = new Lazy<IReadOnlyList<EvalQuestion>>(Load);
    }

    public IReadOnlyList<EvalQuestion> Questions => _questions.Value;

    public IReadOnlyList<string> CorpusFileNames =>
        Questions
            .Where(question => question.Answerable)
            .SelectMany(question => question.ExpectedFiles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private IReadOnlyList<EvalQuestion> Load()
    {
        var json = File.ReadAllText(_path);
        var questions = JsonSerializer.Deserialize<List<EvalQuestion>>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Evaluation questions in '{_path}' were empty.");

        if (questions.Count == 0)
            throw new InvalidOperationException($"Evaluation questions in '{_path}' were empty.");

        return questions;
    }

    private static string ResolvePath(string contentRoot)
    {
        var candidates = new[]
        {
            Path.Combine(contentRoot, "Eval", "federalist-questions.json"),
            Path.Combine(AppContext.BaseDirectory, "Eval", "federalist-questions.json")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException(
            "Could not find Eval/federalist-questions.json.",
            candidates[0]);
    }
}
