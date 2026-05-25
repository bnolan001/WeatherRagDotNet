using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherRag.Rag.Options;
using WeatherRag.Rag.Services;

namespace WeatherRag.Rag;

public static class DependencyInjection
{
    public static IServiceCollection AddRagServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName));
        services.Configure<ChunkingOptions>(configuration.GetSection(ChunkingOptions.SectionName));
        services.Configure<EmbeddingOptions>(configuration.GetSection(EmbeddingOptions.SectionName));
        services.Configure<VectorStoreOptions>(configuration.GetSection(VectorStoreOptions.SectionName));

        services.AddSingleton<IVectorStore, InMemoryVectorStore>();
        services.AddSingleton<IOnnxSessionFactory, OnnxSessionFactory>();
        services.AddSingleton<OnnxEmbeddingService>();
        services.AddSingleton<IEmbeddingService>(sp => sp.GetRequiredService<OnnxEmbeddingService>());
        services.AddSingleton<IEmbeddingWarmupService>(sp => sp.GetRequiredService<OnnxEmbeddingService>());
        services.AddScoped<IPdfExtractor, PdfPigExtractor>();
        services.AddScoped<IChunkingService, TextChunkingService>();
        services.AddScoped<IRetrievalService, RetrievalService>();
        services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();

        return services;
    }
}
