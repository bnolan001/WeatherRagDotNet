using FluentAssertions;
using Microsoft.ML.OnnxRuntime;
using WeatherRag.Rag.Options;
using WeatherRag.Rag.Services;

namespace WeatherRag.Tests.Rag;

public sealed class OnnxSessionFactoryTests
{
    [Fact]
    public void BuildProviderPriority_DefaultsToOpenVinoDirectMlAndCpu()
    {
        var options = new EmbeddingOptions
        {
            ProviderPriority = [],
            EnableCpuFallback = true
        };

        var providers = OnnxSessionFactory.BuildProviderPriority(options);

        providers.Should().ContainInOrder("OpenVINO", "DirectML", "CPU");
    }

    [Fact]
    public void BuildProviderPriority_RemovesDuplicates_AndHonorsCpuFallbackSetting()
    {
        var options = new EmbeddingOptions
        {
            ProviderPriority = ["OpenVINO", "DML", "CPU", "DirectML"],
            EnableCpuFallback = false
        };

        var providers = OnnxSessionFactory.BuildProviderPriority(options);

        providers.Should().ContainInOrder("OpenVINO", "DirectML");
        providers.Should().NotContain("CPU");
    }

    [Theory]
    [InlineData("ORT_DISABLE_ALL", GraphOptimizationLevel.ORT_DISABLE_ALL)]
    [InlineData("ENABLE_BASIC", GraphOptimizationLevel.ORT_ENABLE_BASIC)]
    [InlineData("ORT_ENABLE_EXTENDED", GraphOptimizationLevel.ORT_ENABLE_EXTENDED)]
    [InlineData("ORT_ENABLE_ALL", GraphOptimizationLevel.ORT_ENABLE_ALL)]
    [InlineData("invalid-value", GraphOptimizationLevel.ORT_ENABLE_ALL)]
    public void ResolveGraphOptimizationLevel_MapsConfiguredValues(string configuredValue, GraphOptimizationLevel expected)
    {
        var resolved = OnnxSessionFactory.ResolveGraphOptimizationLevel(configuredValue);

        resolved.Should().Be(expected);
    }
}
