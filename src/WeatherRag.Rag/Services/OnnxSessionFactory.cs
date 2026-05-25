using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using WeatherRag.Rag.Options;

namespace WeatherRag.Rag.Services;

public sealed class OnnxSessionFactory : IOnnxSessionFactory
{
    private static readonly IReadOnlyList<string> DefaultProviderPriority = ["OpenVINO", "DirectML", "CPU"];

    private readonly EmbeddingOptions _options;
    private readonly ILogger<OnnxSessionFactory> _logger;

    public OnnxSessionFactory(IOptions<EmbeddingOptions> options, ILogger<OnnxSessionFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public OnnxSessionContext CreateSession(string modelPath)
    {
        var resolvedModelPath = Path.GetFullPath(modelPath);
        if (!File.Exists(resolvedModelPath))
            throw new FileNotFoundException($"Embedding model was not found at '{resolvedModelPath}'.", resolvedModelPath);

        var providers = BuildProviderPriority(_options);
        var failures = new List<Exception>();

        foreach (var provider in providers)
        {
            var sessionOptions = CreateSessionOptions();

            try
            {
                AppendProvider(sessionOptions, provider);
                var session = new InferenceSession(resolvedModelPath, sessionOptions);
                _logger.LogInformation(
                    "Initialized ONNX embedding session using {Provider} for {ModelPath}.",
                    provider,
                    resolvedModelPath);

                return new OnnxSessionContext(session, sessionOptions, provider);
            }
            catch (Exception ex)
            {
                sessionOptions.Dispose();
                failures.Add(new InvalidOperationException($"Provider '{provider}' failed to initialize.", ex));
                _logger.LogWarning(
                    ex,
                    "ONNX provider {Provider} was unavailable. Attempting next provider.",
                    provider);
            }
        }

        throw new AggregateException(
            $"Unable to initialize ONNX inference session for model '{resolvedModelPath}' with configured providers.",
            failures);
    }

    public static IReadOnlyList<string> BuildProviderPriority(EmbeddingOptions options)
    {
        var configured = (options.ProviderPriority ?? [])
            .Select(NormalizeProviderName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        if (configured.Count == 0)
            configured.AddRange(DefaultProviderPriority);

        var ordered = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in configured)
        {
            if (seen.Add(provider))
                ordered.Add(provider);
        }

        if (options.EnableCpuFallback)
        {
            if (!ordered.Contains("CPU", StringComparer.OrdinalIgnoreCase))
                ordered.Add("CPU");
        }
        else
        {
            ordered.RemoveAll(provider => provider.Equals("CPU", StringComparison.OrdinalIgnoreCase));
        }

        if (ordered.Count == 0)
            throw new InvalidOperationException("At least one valid embedding execution provider must be configured.");

        return ordered;
    }

    public static GraphOptimizationLevel ResolveGraphOptimizationLevel(string? value)
    {
        return value?.Trim().ToUpperInvariant() switch
        {
            "ORT_DISABLE_ALL" or "DISABLE_ALL" => GraphOptimizationLevel.ORT_DISABLE_ALL,
            "ORT_ENABLE_BASIC" or "ENABLE_BASIC" => GraphOptimizationLevel.ORT_ENABLE_BASIC,
            "ORT_ENABLE_EXTENDED" or "ENABLE_EXTENDED" => GraphOptimizationLevel.ORT_ENABLE_EXTENDED,
            "ORT_ENABLE_ALL" or "ENABLE_ALL" or null or "" => GraphOptimizationLevel.ORT_ENABLE_ALL,
            _ => GraphOptimizationLevel.ORT_ENABLE_ALL
        };
    }

    private SessionOptions CreateSessionOptions()
    {
        var sessionOptions = new SessionOptions
        {
            EnableMemoryPattern = _options.EnableMemoryPattern,
            EnableCpuMemArena = _options.EnableCpuMemArena,
            GraphOptimizationLevel = ResolveGraphOptimizationLevel(_options.GraphOptimizationLevel)
        };

        if (_options.IntraOpThreads > 0)
            sessionOptions.IntraOpNumThreads = _options.IntraOpThreads;

        if (_options.InterOpThreads > 0)
            sessionOptions.InterOpNumThreads = _options.InterOpThreads;

        return sessionOptions;
    }

    private void AppendProvider(SessionOptions sessionOptions, string provider)
    {
        switch (provider.ToUpperInvariant())
        {
            case "OPENVINO":
                AppendOpenVino(sessionOptions, _options.OpenVinoDeviceType);
                break;
            case "DIRECTML":
                sessionOptions.AppendExecutionProvider_DML(_options.DirectMlDeviceId);
                break;
            case "CPU":
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported execution provider '{provider}'. Supported values are OpenVINO, DirectML, and CPU.");
        }
    }

    private static void AppendOpenVino(SessionOptions sessionOptions, string deviceType)
    {
        var method = typeof(SessionOptions)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == "AppendExecutionProvider_OpenVINO")
            .OrderBy(m => m.GetParameters().Length)
            .FirstOrDefault();

        if (method is null)
        {
            throw new NotSupportedException(
                "OpenVINO execution provider is not available in the active ONNX Runtime build.");
        }

        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];
        var assignedDeviceType = false;

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (!assignedDeviceType && parameter.ParameterType == typeof(string))
            {
                args[i] = deviceType;
                assignedDeviceType = true;
                continue;
            }

            if (parameter.HasDefaultValue)
            {
                args[i] = parameter.DefaultValue;
                continue;
            }

            args[i] = parameter.ParameterType.IsValueType
                ? Activator.CreateInstance(parameter.ParameterType)
                : null;
        }

        try
        {
            method.Invoke(sessionOptions, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static string NormalizeProviderName(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            return string.Empty;

        if (providerName.Equals("DML", StringComparison.OrdinalIgnoreCase))
            return "DirectML";

        if (providerName.Equals("OPEN_VINO", StringComparison.OrdinalIgnoreCase))
            return "OpenVINO";

        return providerName.Trim();
    }
}
