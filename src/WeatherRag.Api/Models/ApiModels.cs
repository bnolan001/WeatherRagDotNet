namespace WeatherRag.Api.Models;

public sealed record QueryRequest
{
    public required string Query { get; init; }
    public string? ModelId { get; init; }
}

public sealed record CitationDto
{
    public required string SourceFile { get; init; }
    public required int PageNumber { get; init; }
    public required string SectionHint { get; init; }
    public required float Score { get; init; }
}

public sealed record QueryResponse
{
    public required string Answer { get; init; }
    public required IReadOnlyList<CitationDto> Citations { get; init; }
    public bool IsGrounded { get; init; }
    public long ElapsedMs { get; init; }
}

public sealed record ModelInfoDto
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsDefault { get; init; }
}

public sealed record IndexStatusResponse
{
    public required int ChunkCount { get; init; }
    public required string StorePath { get; init; }
}

public sealed record IngestResponse
{
    public required string FileName { get; init; }
    public required string Message { get; init; }
}

public sealed record RemoveResponse
{
    public required string FileName { get; init; }
    public required string Message { get; init; }
}
