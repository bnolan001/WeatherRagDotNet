namespace WeatherRag.Rag.Services;

public interface IEmbeddingWarmupService
{
    ValueTask WarmupAsync(CancellationToken cancellationToken = default);
}
