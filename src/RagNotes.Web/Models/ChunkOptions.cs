namespace RagNotes.Web.Models;

public sealed record ChunkOptions(int MaxTokens, int OverlapPercent, bool PreferSentences)
{
  public const int DefaultMaxTokens = 256;
  public const int DefaultOverlapPercent = 20;
  public const bool DefaultPreferSentences = true;

  public const int MinMaxTokens = 32;
  public const int MaxMaxTokens = 2048;
  public const int MinOverlapPercent = 0;
  public const int MaxOverlapPercent = 50;

  public static ChunkOptions Default { get; } = new(
      DefaultMaxTokens,
      DefaultOverlapPercent,
      DefaultPreferSentences);

  public int OverlapTokens
  {
    get
    {
      if (MaxTokens <= 1)
      {
        return 0;
      }

      var overlap = MaxTokens * OverlapPercent / 100;
      return Math.Clamp(overlap, 0, MaxTokens - 1);
    }
  }

  public string Summary =>
      $"{MaxTokens} tokens, {OverlapPercent}% overlap, {(PreferSentences ? "sentences" : "token windows")}";

  public static string? Validate(int maxTokens, int overlapPercent)
  {
    if (maxTokens < MinMaxTokens || maxTokens > MaxMaxTokens)
    {
      return $"Max tokens must be between {MinMaxTokens} and {MaxMaxTokens}.";
    }

    if (overlapPercent < MinOverlapPercent || overlapPercent > MaxOverlapPercent)
    {
      return $"Overlap percent must be between {MinOverlapPercent} and {MaxOverlapPercent}.";
    }

    return null;
  }
}
