using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using WeatherRag.Rag.Options;

namespace WeatherRag.Rag.Services;

public sealed class OnnxEmbeddingService : IEmbeddingService, IEmbeddingWarmupService, IDisposable
{
    private readonly EmbeddingOptions _options;
    private readonly IOnnxSessionFactory _sessionFactory;
    private readonly ILogger<OnnxEmbeddingService> _logger;
    private OnnxSessionContext? _sessionContext;
    private BertTokenizer? _tokenizer;
    private readonly object _sessionLock = new();
    private readonly object _tokenizerLock = new();
    // DirectML is not thread-safe for concurrent Run() calls on the same session.
    private readonly SemaphoreSlim _runLock = new(1, 1);

    public OnnxEmbeddingService(
        IOptions<EmbeddingOptions> options,
        IOnnxSessionFactory sessionFactory,
        ILogger<OnnxEmbeddingService> logger)
    {
        _options = options.Value;
        _sessionFactory = sessionFactory;
        _logger = logger;
    }

    private OnnxSessionContext SessionContext
    {
        get
        {
            if (_sessionContext is null)
            {
                lock (_sessionLock)
                {
                    if (_sessionContext is null)
                    {
                        _sessionContext = _sessionFactory.CreateSession(_options.ModelPath);
                        _logger.LogInformation(
                            "Embedding model loaded from {Path} using {Provider} execution provider.",
                            Path.GetFullPath(_options.ModelPath),
                            _sessionContext.ProviderName);
                    }
                }
            }

            return _sessionContext;
        }
    }

    private BertTokenizer Tokenizer
    {
        get
        {
            if (_tokenizer is null)
            {
                lock (_tokenizerLock)
                {
                    if (_tokenizer is null)
                    {
                        var vocabPath = Path.GetFullPath(_options.VocabPath);
                        _logger.LogInformation("Loading BERT tokenizer vocab from {Path}", vocabPath);
                        if (!File.Exists(vocabPath))
                            throw new FileNotFoundException(
                                $"BERT vocabulary file not found at '{vocabPath}'. " +
                                "Place the vocab.txt for your embedding model (e.g. all-MiniLM-L6-v2) at that path.",
                                vocabPath);
                        _tokenizer = BertTokenizer.Create(vocabPath, new BertOptions
                        {
                            LowerCaseBeforeTokenization = true
                        });
                    }
                }
            }
            return _tokenizer;
        }
    }

    public async ValueTask WarmupAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableWarmup)
            return;

        _logger.LogInformation("Starting embedding model warmup.");
        _ = await GenerateAsync("WARMUP METAR KOFF 101755Z AUTO 22015KT 10SM CLR 31/13 A2992", cancellationToken);
        _logger.LogInformation("Embedding model warmup complete.");
    }

    public async ValueTask<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        var results = await GenerateBatchAsync([text], cancellationToken);
        return results[0];
    }

    public async ValueTask<IReadOnlyList<float[]>> GenerateBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        var embeddings = new List<float[]>(textList.Count);
        var stopwatch = Stopwatch.StartNew();

        foreach (var text in textList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tokens = Tokenize(text, _options.MaxSequenceLength);
            var inputIds = new DenseTensor<long>(new[] { 1, tokens.Length });
            var attentionMask = new DenseTensor<long>(new[] { 1, tokens.Length });
            var tokenTypeIds = new DenseTensor<long>(new[] { 1, tokens.Length });

            for (int i = 0; i < tokens.Length; i++)
            {
                inputIds[0, i] = tokens[i];
                attentionMask[0, i] = 1L;
                tokenTypeIds[0, i] = 0L;
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
                NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds)
            };

            await _runLock.WaitAsync(cancellationToken);
            try
            {
                using var outputs = SessionContext.Session.Run(inputs);
                var outputTensor = outputs.First().AsTensor<float>();
                var embedding = MeanPool(outputTensor, tokens.Length);
                embeddings.Add(Normalize(embedding));
            }
            finally
            {
                _runLock.Release();
            }
        }

        stopwatch.Stop();
        _logger.LogDebug(
            "Generated {EmbeddingCount} embeddings in {ElapsedMs} ms using {Provider}.",
            embeddings.Count,
            stopwatch.ElapsedMilliseconds,
            SessionContext.ProviderName);

        return embeddings;
    }

    private long[] Tokenize(string text, int maxLength)
    {
        var ids = Tokenizer.EncodeToIds(text, considerPreTokenization: true, considerNormalization: true);
        var count = Math.Min(ids.Count, maxLength);
        var tokens = new long[count];
        for (int i = 0; i < count; i++)
            tokens[i] = (long)ids[i];
        return tokens;
    }

    private static float[] MeanPool(Tensor<float> tensor, int seqLen)
    {
        int dims = (int)(tensor.Length / tensor.Dimensions[1]);
        var pooled = new float[dims];
        for (int t = 0; t < seqLen; t++)
            for (int d = 0; d < dims; d++)
                pooled[d] += tensor[0, t, d];
        for (int d = 0; d < dims; d++)
            pooled[d] /= seqLen;
        return pooled;
    }

    private static float[] Normalize(float[] vector)
    {
        float norm = MathF.Sqrt(vector.Sum(v => v * v));
        if (norm < 1e-8f) return vector;
        return vector.Select(v => v / norm).ToArray();
    }

    public void Dispose()
    {
        _sessionContext?.Dispose();
        _sessionContext = null;
        _runLock.Dispose();
    }
}
