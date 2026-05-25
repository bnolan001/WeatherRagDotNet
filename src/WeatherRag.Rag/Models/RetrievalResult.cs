namespace WeatherRag.Rag.Models;

public sealed record RetrievalResult
{
    public required DocumentChunk Chunk { get; init; }
    public required float Score { get; init; }
}
