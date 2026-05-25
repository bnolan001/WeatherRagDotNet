using Microsoft.Extensions.Options;
using WeatherRag.Rag.Models;
using WeatherRag.Rag.Options;

namespace WeatherRag.Rag.Services;

public sealed class TextChunkingService : IChunkingService
{
    private readonly ChunkingOptions _options;

    public TextChunkingService(IOptions<ChunkingOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<DocumentChunk> Chunk(
        string sourceFile,
        int pageNumber,
        string text,
        IReadOnlyList<ImageReference> images)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<DocumentChunk>();
        int step = Math.Max(1, _options.MaxTokens - _options.OverlapTokens);
        int chunkIndex = 0;

        for (int i = 0; i < words.Length; i += step)
        {
            var slice = words.Skip(i).Take(_options.MaxTokens).ToArray();
            if (slice.Length == 0) break;

            chunks.Add(new DocumentChunk
            {
                Id = $"{Path.GetFileNameWithoutExtension(sourceFile)}_p{pageNumber}_c{chunkIndex}",
                SourceFile = sourceFile,
                PageNumber = pageNumber,
                Text = string.Join(' ', slice),
                ChunkIndex = chunkIndex,
                SectionHint = string.Empty,
                Images = chunkIndex == 0 ? images : []
            });
            chunkIndex++;
        }

        return chunks;
    }
}
