namespace WeatherRag.Inference.Options;

public sealed class ModelProfile
{
    public string DisplayName { get; set; } = string.Empty;
    public string ModelPath { get; set; } = "models/llm/model.gguf";
    public int ContextSize { get; set; } = 4096;
    public int MaxTokens { get; set; } = 1024;
    public float Temperature { get; set; } = 0.2f;
    public int GpuLayerCount { get; set; } = 0;
    public int Threads { get; set; } = 8;
}

public sealed class InferenceOptions
{
    public const string SectionName = "Inference";
    public string DefaultModelId { get; set; } = "default";
    public Dictionary<string, ModelProfile> Models { get; set; } = new();
}
