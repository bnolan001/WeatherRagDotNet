using FluentAssertions;
using Microsoft.Extensions.Options;
using WeatherRag.Rag.Models;
using WeatherRag.Rag.Options;
using WeatherRag.Rag.Services;

namespace WeatherRag.Tests.Rag;

public sealed class TextChunkingServiceTests
{
    private static TextChunkingService CreateService(int maxTokens = 10, int overlapTokens = 2)
    {
        var options = Options.Create(new ChunkingOptions { MaxTokens = maxTokens, OverlapTokens = overlapTokens });
        return new TextChunkingService(options);
    }

    [Fact]
    public void Chunk_EmptyText_ReturnsEmpty()
    {
        var svc = CreateService();
        var result = svc.Chunk("test.pdf", 1, string.Empty, []);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Chunk_WhitespaceText_ReturnsEmpty()
    {
        var svc = CreateService();
        var result = svc.Chunk("test.pdf", 1, "   \n  ", []);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Chunk_ShortText_ReturnsSingleChunk()
    {
        var svc = CreateService(maxTokens: 20);
        var text = "METAR KLAX 121853Z 25015KT 10SM FEW020 18/12 A2990";
        var result = svc.Chunk("test.pdf", 1, text, []);
        result.Should().HaveCount(1);
        result[0].Text.Should().Contain("METAR");
    }

    [Fact]
    public void Chunk_LongText_ReturnsMultipleChunks()
    {
        var svc = CreateService(maxTokens: 5, overlapTokens: 1);
        var words = Enumerable.Range(1, 20).Select(i => $"word{i}");
        var text = string.Join(' ', words);
        var result = svc.Chunk("test.pdf", 1, text, []);
        result.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void Chunk_SetsProvenanceMetadata()
    {
        var svc = CreateService(maxTokens: 100);
        var result = svc.Chunk("weather_pub.pdf", 3, "TAF KLAX 121730Z 1218/1318 25012KT P6SM SKC", []);
        result[0].SourceFile.Should().Be("weather_pub.pdf");
        result[0].PageNumber.Should().Be(3);
        result[0].ChunkIndex.Should().Be(0);
    }

    [Fact]
    public void Chunk_ImagesOnlyOnFirstChunk()
    {
        var svc = CreateService(maxTokens: 3, overlapTokens: 0);
        var images = new List<ImageReference>
        {
            new() { SourceFile = "test.pdf", PageNumber = 1, ImageIndex = 0, Width = 100, Height = 100 }
        };
        var words = Enumerable.Range(1, 12).Select(i => $"w{i}");
        var result = svc.Chunk("test.pdf", 1, string.Join(' ', words), images);
        result[0].Images.Should().HaveCount(1);
        result.Skip(1).All(c => c.Images.Count == 0).Should().BeTrue();
    }
}
