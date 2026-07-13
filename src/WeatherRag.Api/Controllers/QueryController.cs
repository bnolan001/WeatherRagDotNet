using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WeatherRag.Api.Models;
using WeatherRag.Inference.Models;
using WeatherRag.Inference.Services;
using WeatherRag.Rag.Services;

namespace WeatherRag.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class QueryController : ControllerBase
{
    private readonly IRetrievalService _retrieval;
    private readonly IInferenceService _inference;
    private readonly ILogger<QueryController> _logger;

    public QueryController(IRetrievalService retrieval, IInferenceService inference, ILogger<QueryController> logger)
    {
        _retrieval = retrieval;
        _inference = inference;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Query([FromBody] QueryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest(new { message = "Query cannot be empty." });

        var totalStopwatch = Stopwatch.StartNew();

        var retrievalStopwatch = Stopwatch.StartNew();
        var results = await _retrieval.RetrieveAsync(request.Query, cancellationToken);
        retrievalStopwatch.Stop();
        _logger.LogInformation("Query retrieval stage completed in {ElapsedMs} ms", retrievalStopwatch.ElapsedMilliseconds);

        if (results.Count == 0)
        {
            totalStopwatch.Stop();
            return Ok(new QueryResponse
            {
                Answer = "No relevant passages found in the indexed documents for this query.",
                Citations = [],
                IsGrounded = false,
                ElapsedMs = totalStopwatch.ElapsedMilliseconds
            });
        }

        var generationRequest = new GenerationRequest
        {
            Query = request.Query,
            ContextPassages = results.Select(r => r.Chunk.Text).ToList(),
            Citations = results.Select(r =>
                $"{Path.GetFileName(r.Chunk.SourceFile)}, page {r.Chunk.PageNumber}").ToList(),
            ModelId = request.ModelId
        };

        var generationStopwatch = Stopwatch.StartNew();
        var response = await _inference.GenerateAsync(generationRequest, cancellationToken);
        generationStopwatch.Stop();
        totalStopwatch.Stop();
        _logger.LogInformation("Query generation stage completed in {ElapsedMs} ms", generationStopwatch.ElapsedMilliseconds);
        _logger.LogInformation("Query total elapsed time {ElapsedMs} ms", totalStopwatch.ElapsedMilliseconds);

        var citations = results.Select(r => new CitationDto
        {
            SourceFile = Path.GetFileName(r.Chunk.SourceFile),
            PageNumber = r.Chunk.PageNumber,
            SectionHint = r.Chunk.SectionHint,
            Score = r.Score
        }).ToList();

        return Ok(new QueryResponse
        {
            Answer = response.Answer,
            Citations = citations,
            IsGrounded = response.IsGrounded,
            ElapsedMs = totalStopwatch.ElapsedMilliseconds
        });
    }
}
