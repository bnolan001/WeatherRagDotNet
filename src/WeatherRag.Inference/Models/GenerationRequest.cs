namespace WeatherRag.Inference.Models;

public sealed record GenerationRequest
{
    public required string Query { get; init; }
    public required IReadOnlyList<string> ContextPassages { get; init; }
    public required IReadOnlyList<string> Citations { get; init; }
}
