namespace WeatherRag.Rag.Options;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";
    public string ModelPath { get; set; } = "models/embedding/model.onnx";
    public string VocabPath { get; set; } = "models/embedding/vocab.txt";
    public int Dimensions { get; set; } = 384;
    public int MaxSequenceLength { get; set; } = 512;
    public List<string> ProviderPriority { get; set; } = ["OpenVINO", "DirectML", "CPU"];
    public string OpenVinoDeviceType { get; set; } = "AUTO";
    public int DirectMlDeviceId { get; set; } = 0;
    public bool EnableCpuFallback { get; set; } = true;
    public string GraphOptimizationLevel { get; set; } = "ORT_ENABLE_ALL";
    public int IntraOpThreads { get; set; } = 0;
    public int InterOpThreads { get; set; } = 0;
    public bool EnableMemoryPattern { get; set; } = true;
    public bool EnableCpuMemArena { get; set; } = true;
    public bool EnableWarmup { get; set; } = true;
}
