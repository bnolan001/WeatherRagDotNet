using WeatherRag.Rag.Models;

namespace WeatherRag.Rag.Services;

public interface IPdfExtractor
{
    IAsyncEnumerable<(int Page, string Text, IReadOnlyList<ImageReference> Images)> ExtractPagesAsync(
        string filePath, CancellationToken cancellationToken = default);
}
