namespace WeatherRag.Rag.Options;

public sealed class VectorStoreOptions
{
    public const string SectionName = "VectorStore";
    public string PersistencePath { get; set; } = "data/index/vectors.json";
    public int TopK { get; set; } = 5;
    public float MinScore { get; set; } = 0.3f;
}
