using WeatherRag.Rag.Models;

namespace WeatherRag.Rag.Services;

public interface IChunkingService
{
    IReadOnlyList<DocumentChunk> Chunk(
        string sourceFile,
        int pageNumber,
        string text,
        IReadOnlyList<ImageReference> images);
}
