namespace WeatherRag.Rag.Models;

public sealed record ImageReference
{
    public required string SourceFile { get; init; }
    public required int PageNumber { get; init; }
    public required int ImageIndex { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string? Caption { get; init; }
}
