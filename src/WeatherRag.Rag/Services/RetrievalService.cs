using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeatherRag.Rag.Models;
using WeatherRag.Rag.Options;

namespace WeatherRag.Rag.Services;

public sealed class RetrievalService : IRetrievalService
{
    private readonly IEmbeddingService _embedding;
    private readonly IVectorStore _vectorStore;
    private readonly VectorStoreOptions _options;
    private readonly ILogger<RetrievalService> _logger;

    public RetrievalService(
        IEmbeddingService embedding,
        IVectorStore vectorStore,
        IOptions<VectorStoreOptions> options,
        ILogger<RetrievalService> logger)
    {
        _embedding = embedding;
        _vectorStore = vectorStore;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RetrievalResult>> RetrieveAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var embeddingStopwatch = Stopwatch.StartNew();
        var queryEmbedding = await _embedding.GenerateAsync(query, cancellationToken);
        embeddingStopwatch.Stop();

        var searchStopwatch = Stopwatch.StartNew();
        var results = await _vectorStore.SearchAsync(queryEmbedding, _options.TopK, _options.MinScore, cancellationToken);
        searchStopwatch.Stop();

        _logger.LogInformation(
            "Retrieval stage timing EmbeddingMs={EmbeddingMs} VectorSearchMs={VectorSearchMs} ResultCount={ResultCount}",
            embeddingStopwatch.ElapsedMilliseconds,
            searchStopwatch.ElapsedMilliseconds,
            results.Count);

        return results;
    }
}
