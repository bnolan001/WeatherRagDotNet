using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WeatherRag.Rag.Models;
using WeatherRag.Rag.Options;
using WeatherRag.Rag.Services;

namespace WeatherRag.Tests.Rag;

public sealed class RetrievalServiceTests
{
    [Fact]
    public async Task RetrieveAsync_DelegatesToEmbeddingAndVectorStore()
    {
        const string query = "What is the standard TAF period?";
        float[] queryEmbed = [0.1f, 0.2f, 0.3f];

        var mockEmbedding = new Mock<IEmbeddingService>();
        mockEmbedding
            .Setup(e => e.GenerateAsync(query, default))
            .ReturnsAsync(queryEmbed);

        var expectedChunk = new DocumentChunk
        {
            Id = "doc_p1_c0",
            SourceFile = "weather.pdf",
            PageNumber = 1,
            Text = "TAF standard period is 24 hours.",
            ChunkIndex = 0,
            SectionHint = string.Empty
        };
        var expectedResults = new List<RetrievalResult>
        {
            new() { Chunk = expectedChunk, Score = 0.85f }
        };

        var options = Options.Create(new VectorStoreOptions { TopK = 5, MinScore = 0.3f });

        var mockStore = new Mock<IVectorStore>();
        mockStore
            .Setup(s => s.SearchAsync(queryEmbed, 5, 0.3f, default))
            .ReturnsAsync(expectedResults);

        var svc = new RetrievalService(
            mockEmbedding.Object,
            mockStore.Object,
            options,
            NullLogger<RetrievalService>.Instance);
        var results = await svc.RetrieveAsync(query);

        results.Should().HaveCount(1);
        results[0].Score.Should().BeApproximately(0.85f, 0.001f);
        results[0].Chunk.Text.Should().Contain("TAF");
    }
}
