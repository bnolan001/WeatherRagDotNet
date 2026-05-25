namespace WeatherRag.Rag.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";
    public string DocumentStorePath { get; set; } = "data/documents";
    public string IndexStorePath { get; set; } = "data/index";
}
