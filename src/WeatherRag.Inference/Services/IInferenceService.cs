using WeatherRag.Inference.Models;

namespace WeatherRag.Inference.Services;

public interface IInferenceService
{
    Task<GenerationResponse> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamAsync(GenerationRequest request, CancellationToken cancellationToken = default);
}
