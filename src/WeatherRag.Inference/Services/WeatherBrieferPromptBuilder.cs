using System.Text;
using WeatherRag.Inference.Models;

namespace WeatherRag.Inference.Services;

public sealed class WeatherBrieferPromptBuilder
{
    private const string SystemPrompt = """
        You are a Senior Air Force Weather Forecaster, Observer, and Briefer.
        Your duty is to provide accurate, operationally relevant weather analysis
        and forecasts grounded strictly in the provided reference passages.

        Rules of engagement:
        - Base every answer exclusively on the context passages provided.
        - If the provided context does not contain sufficient information to answer,
          state: "Insufficient data in reference material to support this assessment."
        - Use Air Force weather terminology: METAR, TAF, SIGMET, PIREP, OWS, FLIP, etc.
        - Cite the source document and page number for every key claim.
        - Express wind in knots, temperature in Celsius (military standard), and
          visibility in statute miles or meters as appropriate.
        - Maintain a professional, concise, mission-focused tone at all times.
        - Never speculate or draw on knowledge outside the provided context.
        """;

    public string Build(GenerationRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine(SystemPrompt);
        sb.AppendLine();
        sb.AppendLine("=== REFERENCE PASSAGES ===");

        for (int i = 0; i < request.ContextPassages.Count; i++)
        {
            sb.AppendLine($"[{i + 1}] {request.Citations.ElementAtOrDefault(i) ?? "Unknown source"}");
            sb.AppendLine(request.ContextPassages[i]);
            sb.AppendLine();
        }

        sb.AppendLine("=== WEATHER QUERY ===");
        sb.AppendLine(request.Query);
        sb.AppendLine();
        sb.AppendLine("=== WEATHER ASSESSMENT ===");

        return sb.ToString();
    }
}
