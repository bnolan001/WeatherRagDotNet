using FluentAssertions;
using WeatherRag.Inference.Models;
using WeatherRag.Inference.Services;

namespace WeatherRag.Tests.Inference;

public sealed class WeatherBrieferPromptBuilderTests
{
    private readonly WeatherBrieferPromptBuilder _builder = new();

    [Fact]
    public void Build_ContainsSystemPersona()
    {
        var request = new GenerationRequest
        {
            Query = "Describe METAR observation intervals.",
            ContextPassages = [],
            Citations = []
        };
        var prompt = _builder.Build(request);
        prompt.Should().Contain("Senior Air Force Weather Forecaster");
    }

    [Fact]
    public void Build_IncludesQueryText()
    {
        var request = new GenerationRequest
        {
            Query = "What is a SIGMET?",
            ContextPassages = [],
            Citations = []
        };
        var prompt = _builder.Build(request);
        prompt.Should().Contain("What is a SIGMET?");
    }

    [Fact]
    public void Build_IncludesContextPassagesWithCitations()
    {
        var request = new GenerationRequest
        {
            Query = "Ceiling definitions?",
            ContextPassages = ["A ceiling is the lowest broken or overcast layer."],
            Citations = ["AFH 11-203V1, page 42"]
        };
        var prompt = _builder.Build(request);
        prompt.Should().Contain("AFH 11-203V1, page 42");
        prompt.Should().Contain("lowest broken or overcast layer");
    }

    [Fact]
    public void Build_ContainsInsufficientDataInstruction()
    {
        var request = new GenerationRequest
        {
            Query = "Any query",
            ContextPassages = [],
            Citations = []
        };
        var prompt = _builder.Build(request);
        prompt.Should().Contain("Insufficient data in reference material");
    }
}
