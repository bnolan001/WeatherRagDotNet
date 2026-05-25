namespace WeatherRag.Inference.Models;

public sealed record GenerationResponse
{
    public required string Answer { get; init; }
    public required IReadOnlyList<string> Citations { get; init; }
    public bool IsGrounded { get; init; }
}
