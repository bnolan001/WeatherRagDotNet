namespace WeatherRag.Rag.Services;

public interface IDocumentIngestionService
{
    Task IngestAsync(string filePath, CancellationToken cancellationToken = default);
    Task RemoveAsync(string filePath, CancellationToken cancellationToken = default);
}
