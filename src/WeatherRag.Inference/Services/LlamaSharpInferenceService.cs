using System.Runtime.CompilerServices;
using LLama;
using LLama.Common;
using LLama.Sampling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeatherRag.Inference.Models;
using WeatherRag.Inference.Options;

namespace WeatherRag.Inference.Services;

public sealed class LlamaSharpInferenceService : IInferenceService, IDisposable
{
    private readonly InferenceOptions _options;
    private readonly WeatherBrieferPromptBuilder _promptBuilder;
    private readonly ILogger<LlamaSharpInferenceService> _logger;
    private LLamaWeights? _weights;
    private ModelParams? _modelParams;
    private readonly object _loadLock = new();

    public LlamaSharpInferenceService(
        IOptions<InferenceOptions> options,
        WeatherBrieferPromptBuilder promptBuilder,
        ILogger<LlamaSharpInferenceService> logger)
    {
        _options = options.Value;
        _promptBuilder = promptBuilder;
        _logger = logger;
    }

    private (LLamaWeights Weights, ModelParams Params) GetWeights()
    {
        if (_weights is null || _modelParams is null)
        {
            lock (_loadLock)
            {
                if (_weights is null || _modelParams is null)
                {
                    var modelPath = Path.GetFullPath(_options.ModelPath);
                    _logger.LogInformation("Loading LLaMA model from {Path}", modelPath);

                    _modelParams = new ModelParams(modelPath)
                    {
                        ContextSize = (uint)_options.ContextSize,
                        GpuLayerCount = _options.GpuLayerCount,
                        Threads = _options.Threads
                    };

                    _weights = LLamaWeights.LoadFromFile(_modelParams);
                }
            }
        }
        return (_weights, _modelParams);
    }

    public async Task<GenerationResponse> GenerateAsync(
        GenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var sb = new System.Text.StringBuilder();
        await foreach (var token in StreamAsync(request, cancellationToken))
            sb.Append(token);

        return new GenerationResponse
        {
            Answer = sb.ToString().Trim(),
            Citations = request.Citations,
            IsGrounded = request.ContextPassages.Count > 0
        };
    }

    public async IAsyncEnumerable<string> StreamAsync(
        GenerationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var prompt = _promptBuilder.Build(request);
        var (weights, modelParams) = GetWeights();

        var executor = new StatelessExecutor(weights, modelParams, _logger);
        var inferenceParams = new InferenceParams
        {
            MaxTokens = _options.MaxTokens,
            AntiPrompts = ["=== WEATHER QUERY ===", "[INST]"],
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = _options.Temperature
            }
        };

        await foreach (var token in executor.InferAsync(prompt, inferenceParams, cancellationToken))
            yield return token;
    }

    public void Dispose()
    {
        _weights?.Dispose();
        _weights = null;
    }
}
