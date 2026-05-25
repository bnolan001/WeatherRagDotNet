namespace WeatherRag.Rag.Options;

public sealed class ChunkingOptions
{
    public const string SectionName = "Chunking";
    public int MaxTokens { get; set; } = 512;
    public int OverlapTokens { get; set; } = 64;
}
