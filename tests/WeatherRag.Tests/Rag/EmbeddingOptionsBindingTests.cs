using FluentAssertions;
using Microsoft.Extensions.Configuration;
using WeatherRag.Rag.Options;

namespace WeatherRag.Tests.Rag;

public sealed class EmbeddingOptionsBindingTests
{
    [Fact]
    public void Binding_MapsExecutionProviderConfiguration()
    {
        var data = new Dictionary<string, string?>
        {
            ["Embedding:ProviderPriority:0"] = "OpenVINO",
            ["Embedding:ProviderPriority:1"] = "DirectML",
            ["Embedding:ProviderPriority:2"] = "CPU",
            ["Embedding:OpenVinoDeviceType"] = "AUTO",
            ["Embedding:DirectMlDeviceId"] = "1",
            ["Embedding:EnableCpuFallback"] = "true",
            ["Embedding:GraphOptimizationLevel"] = "ORT_ENABLE_EXTENDED",
            ["Embedding:IntraOpThreads"] = "4",
            ["Embedding:InterOpThreads"] = "2",
            ["Embedding:EnableMemoryPattern"] = "false",
            ["Embedding:EnableCpuMemArena"] = "false",
            ["Embedding:EnableWarmup"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();

        var options = new EmbeddingOptions();
        configuration.GetSection(EmbeddingOptions.SectionName).Bind(options);

        options.ProviderPriority.Should().ContainInOrder("OpenVINO", "DirectML", "CPU");
        options.OpenVinoDeviceType.Should().Be("AUTO");
        options.DirectMlDeviceId.Should().Be(1);
        options.EnableCpuFallback.Should().BeTrue();
        options.GraphOptimizationLevel.Should().Be("ORT_ENABLE_EXTENDED");
        options.IntraOpThreads.Should().Be(4);
        options.InterOpThreads.Should().Be(2);
        options.EnableMemoryPattern.Should().BeFalse();
        options.EnableCpuMemArena.Should().BeFalse();
        options.EnableWarmup.Should().BeTrue();
    }
}
