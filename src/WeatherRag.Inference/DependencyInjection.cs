using LLama.Native;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherRag.Inference.Options;
using WeatherRag.Inference.Services;

namespace WeatherRag.Inference;

public static class DependencyInjection
{
    public static IServiceCollection AddInferenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Must be called before any LLaMA native library operations.
        // Prefer Vulkan (cross-vendor GPU) with automatic CPU fallback.
        NativeLibraryConfig.All
            .WithVulkan(enable: true)
            .WithAutoFallback(enable: true);

        services.Configure<InferenceOptions>(configuration.GetSection(InferenceOptions.SectionName));
        services.AddSingleton<WeatherBrieferPromptBuilder>();
        services.AddSingleton<IInferenceService, LlamaSharpInferenceService>();
        return services;
    }
}
