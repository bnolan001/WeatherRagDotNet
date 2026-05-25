using WeatherRag.Rag.Models;

namespace WeatherRag.Rag.Services;

public interface IRetrievalService
{
    Task<IReadOnlyList<RetrievalResult>> RetrieveAsync(
        string query,
        CancellationToken cancellationToken = default);
}
