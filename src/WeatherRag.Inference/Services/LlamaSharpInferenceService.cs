using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<string, (LLamaWeights Weights, ModelParams Params)> _weightCache = new();
    private readonly ConcurrentDictionary<string, object> _loadLocks = new();

    public LlamaSharpInferenceService(
        IOptions<InferenceOptions> options,
        WeatherBrieferPromptBuilder promptBuilder,
        ILogger<LlamaSharpInferenceService> logger)
    {
        _options = options.Value;
        _promptBuilder = promptBuilder;
        _logger = logger;
    }

    private ModelProfile ResolveProfile(string? modelId)
    {
        var effectiveId = string.IsNullOrWhiteSpace(modelId) ? _options.DefaultModelId : modelId;

        if (!_options.Models.TryGetValue(effectiveId, out var profile))
        {
            _logger.LogWarning(
                "Model ID '{ModelId}' not found in configuration. Falling back to default model '{DefaultModelId}'.",
                effectiveId,
                _options.DefaultModelId);

            if (!_options.Models.TryGetValue(_options.DefaultModelId, out profile))
            {
                throw new InvalidOperationException(
                    $"Default model '{_options.DefaultModelId}' is not configured in Inference.Models.");
            }
        }

        return profile;
    }

    private (LLamaWeights Weights, ModelParams Params) GetWeightsForProfile(ModelProfile profile, string cacheKey)
    {
        if (_weightCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var loadLock = _loadLocks.GetOrAdd(cacheKey, _ => new object());

        lock (loadLock)
        {
            if (_weightCache.TryGetValue(cacheKey, out cached))
                return cached;

            var modelPath = Path.GetFullPath(profile.ModelPath);
            _logger.LogInformation("Loading LLaMA model '{CacheKey}' from {Path}", cacheKey, modelPath);

            var modelParams = new ModelParams(modelPath)
            {
                ContextSize = (uint)profile.ContextSize,
                GpuLayerCount = profile.GpuLayerCount,
                Threads = profile.Threads
            };

            var weights = LLamaWeights.LoadFromFile(modelParams);
            var entry = (weights, modelParams);
            _weightCache[cacheKey] = entry;

            return entry;
        }
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
        var profile = ResolveProfile(request.ModelId);
        var cacheKey = request.ModelId ?? _options.DefaultModelId;
        var (weights, modelParams) = GetWeightsForProfile(profile, cacheKey);

        var prompt = _promptBuilder.Build(request);
        var executor = new StatelessExecutor(weights, modelParams, _logger);
        var inferenceParams = new InferenceParams
        {
            MaxTokens = profile.MaxTokens,
            AntiPrompts = ["=== WEATHER QUERY ===", "[INST]"],
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = profile.Temperature
            }
        };

        await foreach (var token in executor.InferAsync(prompt, inferenceParams, cancellationToken))
            yield return token;
    }

    public void Dispose()
    {
        foreach (var (weights, _) in _weightCache.Values)
        {
            weights.Dispose();
        }
        _weightCache.Clear();
        _loadLocks.Clear();
    }
}
