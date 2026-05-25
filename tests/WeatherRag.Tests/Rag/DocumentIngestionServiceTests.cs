using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WeatherRag.Rag.Models;
using WeatherRag.Rag.Options;
using WeatherRag.Rag.Services;

namespace WeatherRag.Tests.Rag;

public sealed class DocumentIngestionServiceTests
{
    private static readonly string StoreRoot = Path.GetFullPath("data/documents");

    private static DocumentIngestionService CreateService(
        IPdfExtractor? extractor = null,
        IChunkingService? chunker = null,
        IEmbeddingService? embedding = null,
        IVectorStore? store = null)
    {
        var options = Options.Create(new RagOptions { DocumentStorePath = StoreRoot });
        return new DocumentIngestionService(
            extractor ?? new Mock<IPdfExtractor>().Object,
            chunker ?? new Mock<IChunkingService>().Object,
            embedding ?? new Mock<IEmbeddingService>().Object,
            store ?? new Mock<IVectorStore>().Object,
            options,
            NullLogger<DocumentIngestionService>.Instance);
    }

    [Fact]
    public async Task IngestAsync_PathOutsideStore_ThrowsUnauthorizedAccess()
    {
        var svc = CreateService();
        var act = () => svc.IngestAsync(@"C:\Windows\System32\evil.pdf");
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*outside the permitted document store*");
    }

    [Fact]
    public async Task IngestAsync_ValidFile_EmbeddingsGeneratedForEachChunk()
    {
        var filePath = Path.Combine(StoreRoot, "weather.pdf");
        var chunk = new DocumentChunk
        {
            Id = "weather_p1_c0",
            SourceFile = filePath,
            PageNumber = 1,
            Text = "Surface winds NW at 15 knots, visibility 10 statute miles.",
            ChunkIndex = 0,
            SectionHint = string.Empty
        };

        var chunkerMock = new Mock<IChunkingService>();
        chunkerMock
            .Setup(c => c.Chunk(filePath, 1, It.IsAny<string>(), It.IsAny<IReadOnlyList<ImageReference>>()))
            .Returns([chunk]);

        var embeddingMock = new Mock<IEmbeddingService>();
        embeddingMock
            .Setup(e => e.GenerateBatchAsync(
                It.Is<IEnumerable<string>>(texts => texts.SequenceEqual(new[] { chunk.Text })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new float[] { 0.1f, 0.2f, 0.3f }]);

        var storeMock = new Mock<IVectorStore>();
        storeMock.Setup(s => s.RemoveBySourceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        storeMock.Setup(s => s.AddAsync(It.IsAny<IEnumerable<DocumentChunk>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        storeMock.Setup(s => s.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var extractor = new FakeExtractor((1, chunk.Text, []));
        var svc = CreateService(extractor, chunkerMock.Object, embeddingMock.Object, storeMock.Object);

        await svc.IngestAsync(filePath);

        embeddingMock.Verify(
            e => e.GenerateBatchAsync(
                It.Is<IEnumerable<string>>(texts => texts.SequenceEqual(new[] { chunk.Text })),
                It.IsAny<CancellationToken>()),
            Times.Once);
        storeMock.Verify(s => s.AddAsync(It.IsAny<IEnumerable<DocumentChunk>>(), It.IsAny<CancellationToken>()), Times.Once);
        storeMock.Verify(s => s.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_ReIndexing_RemovesExistingChunksFirst()
    {
        var filePath = Path.Combine(StoreRoot, "weather.pdf");

        var storeMock = new Mock<IVectorStore>();
        storeMock.Setup(s => s.RemoveBySourceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        storeMock.Setup(s => s.AddAsync(It.IsAny<IEnumerable<DocumentChunk>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        storeMock.Setup(s => s.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var chunkerMock = new Mock<IChunkingService>();
        chunkerMock.Setup(c => c.Chunk(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<ImageReference>>()))
            .Returns([]);

        var extractor = new FakeExtractor((1, "TAF KLAX 121730Z 1218/1318 25012KT.", []));
        var svc = CreateService(extractor, chunkerMock.Object, store: storeMock.Object);

        await svc.IngestAsync(filePath);

        var callOrder = new List<string>();
        storeMock.Verify(s => s.RemoveBySourceAsync(
            It.Is<string>(p => p.Equals(filePath, StringComparison.OrdinalIgnoreCase)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_CallsStoreRemoveAndSave()
    {
        var filePath = Path.Combine(StoreRoot, "old_brief.pdf");

        var storeMock = new Mock<IVectorStore>();
        storeMock.Setup(s => s.RemoveBySourceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        storeMock.Setup(s => s.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var svc = CreateService(store: storeMock.Object);
        await svc.RemoveAsync(filePath);

        storeMock.Verify(s => s.RemoveBySourceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        storeMock.Verify(s => s.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class FakeExtractor : IPdfExtractor
    {
        private readonly IReadOnlyList<(int Page, string Text, IReadOnlyList<ImageReference> Images)> _pages;

        public FakeExtractor(params (int Page, string Text, IReadOnlyList<ImageReference> Images)[] pages)
            => _pages = pages;

        public async IAsyncEnumerable<(int Page, string Text, IReadOnlyList<ImageReference> Images)> ExtractPagesAsync(
            string filePath,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var p in _pages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return p;
                await Task.Yield();
            }
        }
    }
}
