using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeatherRag.Rag.Models;
using WeatherRag.Rag.Options;

namespace WeatherRag.Rag.Services;

public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly VectorStoreOptions _options;
    private readonly ILogger<InMemoryVectorStore> _logger;
    private readonly List<DocumentChunk> _chunks = [];
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public InMemoryVectorStore(IOptions<VectorStoreOptions> options, ILogger<InMemoryVectorStore> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public int Count
    {
        get { lock (_lock) return _chunks.Count; }
    }

    public Task AddAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _chunks.AddRange(chunks);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RetrievalResult>> SearchAsync(
        float[] queryEmbedding,
        int topK,
        float minScore,
        CancellationToken cancellationToken = default)
    {
        List<DocumentChunk> snapshot;
        lock (_lock)
        {
            snapshot = [.. _chunks];
        }

        var results = snapshot
            .Where(c => c.Embedding is not null)
            .Select(c => new RetrievalResult
            {
                Chunk = c,
                Score = CosineSimilarity(queryEmbedding, c.Embedding!)
            })
            .Where(r => r.Score >= minScore)
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<RetrievalResult>>(results);
    }

    public Task RemoveBySourceAsync(string sourceFile, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _chunks.RemoveAll(c => string.Equals(c.SourceFile, sourceFile, StringComparison.OrdinalIgnoreCase));
        }
        return Task.CompletedTask;
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var path = Path.GetFullPath(_options.PersistencePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        List<DocumentChunk> snapshot;
        lock (_lock)
        {
            snapshot = [.. _chunks];
        }

        await using var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(fs, snapshot, JsonOptions, cancellationToken);
        _logger.LogInformation("Vector store saved {Count} chunks to {Path}", snapshot.Count, path);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = Path.GetFullPath(_options.PersistencePath);
        if (!File.Exists(path))
        {
            _logger.LogInformation("No existing vector store found at {Path}. Starting empty.", path);
            return;
        }

        await using var fs = File.OpenRead(path);
        var loaded = await JsonSerializer.DeserializeAsync<List<DocumentChunk>>(fs, JsonOptions, cancellationToken);
        if (loaded is null) return;

        lock (_lock)
        {
            _chunks.Clear();
            _chunks.AddRange(loaded);
        }
        _logger.LogInformation("Vector store loaded {Count} chunks from {Path}", loaded.Count, path);
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        int len = Math.Min(a.Length, b.Length);
        float dot = 0f, normA = 0f, normB = 0f;
        for (int i = 0; i < len; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        float denom = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return denom < 1e-8f ? 0f : dot / denom;
    }
}
