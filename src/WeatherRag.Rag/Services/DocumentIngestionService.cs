using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeatherRag.Rag.Models;
using WeatherRag.Rag.Options;

namespace WeatherRag.Rag.Services;

public sealed class DocumentIngestionService : IDocumentIngestionService
{
    private readonly IPdfExtractor _extractor;
    private readonly IChunkingService _chunker;
    private readonly IEmbeddingService _embedding;
    private readonly IVectorStore _vectorStore;
    private readonly RagOptions _options;
    private readonly ILogger<DocumentIngestionService> _logger;

    public DocumentIngestionService(
        IPdfExtractor extractor,
        IChunkingService chunker,
        IEmbeddingService embedding,
        IVectorStore vectorStore,
        IOptions<RagOptions> options,
        ILogger<DocumentIngestionService> logger)
    {
        _extractor = extractor;
        _chunker = chunker;
        _embedding = embedding;
        _vectorStore = vectorStore;
        _options = options.Value;
        _logger = logger;
    }

    public async Task IngestAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var sanitized = Path.GetFullPath(filePath);
        var storePath = Path.GetFullPath(_options.DocumentStorePath);

        if (!sanitized.StartsWith(storePath, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"File path '{sanitized}' is outside the permitted document store.");

        _logger.LogInformation("Beginning ingestion of {File}", sanitized);

        var allChunks = new List<DocumentChunk>();

        await foreach (var (page, text, images) in _extractor.ExtractPagesAsync(sanitized, cancellationToken))
        {
            var chunks = _chunker.Chunk(sanitized, page, text, images);
            allChunks.AddRange(chunks);
        }

        _logger.LogInformation("Extracted {ChunkCount} chunks from {File}. Generating embeddings...", allChunks.Count, sanitized);

        var embeddings = await _embedding.GenerateBatchAsync(
            allChunks.Select(c => c.Text),
            cancellationToken);

        for (int i = 0; i < allChunks.Count; i++)
            allChunks[i].Embedding = embeddings[i];

        await _vectorStore.RemoveBySourceAsync(sanitized, cancellationToken);
        await _vectorStore.AddAsync(allChunks, cancellationToken);
        await _vectorStore.SaveAsync(cancellationToken);

        _logger.LogInformation("Ingestion complete for {File}. Total chunks in store: {Total}", sanitized, _vectorStore.Count);
    }

    public async Task RemoveAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var sanitized = Path.GetFullPath(filePath);
        await _vectorStore.RemoveBySourceAsync(sanitized, cancellationToken);
        await _vectorStore.SaveAsync(cancellationToken);
        _logger.LogInformation("Removed document {File} from vector store.", sanitized);
    }
}
