namespace RagNotes.Web.Options;

public class InferenceOptions
{
    public const string SectionName = "Inference";

    public string Provider { get; set; } = "Omlx";
}
