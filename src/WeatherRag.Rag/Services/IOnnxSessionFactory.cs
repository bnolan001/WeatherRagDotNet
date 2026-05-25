using Microsoft.ML.OnnxRuntime;

namespace WeatherRag.Rag.Services;

public interface IOnnxSessionFactory
{
    OnnxSessionContext CreateSession(string modelPath);
}

public sealed class OnnxSessionContext : IDisposable
{
    private readonly SessionOptions _sessionOptions;
    private int _disposed;

    public OnnxSessionContext(InferenceSession session, SessionOptions sessionOptions, string providerName)
    {
        Session = session;
        _sessionOptions = sessionOptions;
        ProviderName = providerName;
    }

    public InferenceSession Session { get; }
    public string ProviderName { get; }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        Session.Dispose();
        _sessionOptions.Dispose();
    }
}
