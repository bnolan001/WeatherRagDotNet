namespace WeatherRag.Rag.Services;

public interface IEmbeddingService
{
    ValueTask<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default);
    ValueTask<IReadOnlyList<float[]>> GenerateBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
