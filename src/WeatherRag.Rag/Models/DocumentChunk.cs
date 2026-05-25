namespace WeatherRag.Rag.Models;

public sealed record DocumentChunk
{
    public required string Id { get; init; }
    public required string SourceFile { get; init; }
    public required int PageNumber { get; init; }
    public required string Text { get; init; }
    public required int ChunkIndex { get; init; }
    public required string SectionHint { get; init; }
    public float[]? Embedding { get; set; }
    public IReadOnlyList<ImageReference> Images { get; init; } = [];
}
