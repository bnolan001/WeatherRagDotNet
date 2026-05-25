using WeatherRag.Rag.Models;

namespace WeatherRag.Rag.Services;

public interface IVectorStore
{
    Task AddAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RetrievalResult>> SearchAsync(float[] queryEmbedding, int topK, float minScore, CancellationToken cancellationToken = default);
    Task RemoveBySourceAsync(string sourceFile, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task LoadAsync(CancellationToken cancellationToken = default);
    int Count { get; }
}
