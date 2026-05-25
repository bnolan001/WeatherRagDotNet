using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WeatherRag.Api.Models;
using WeatherRag.Rag.Options;
using WeatherRag.Rag.Services;

namespace WeatherRag.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class IngestController : ControllerBase
{
    private readonly IDocumentIngestionService _ingestion;
    private readonly RagOptions _ragOptions;
    private readonly ILogger<IngestController> _logger;

    public IngestController(
        IDocumentIngestionService ingestion,
        IOptions<RagOptions> ragOptions,
        ILogger<IngestController> logger)
    {
        _ingestion = ingestion;
        _ragOptions = ragOptions.Value;
        _logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only PDF files are accepted." });

        var storePath = Path.GetFullPath(_ragOptions.DocumentStorePath);
        Directory.CreateDirectory(storePath);

        var safeFileName = Path.GetFileName(file.FileName);
        var destination = Path.Combine(storePath, safeFileName);

        await using (var fs = System.IO.File.Open(destination, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(fs, cancellationToken);
        }

        try
        {
            await _ingestion.IngestAsync(destination, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion failed for {File}", safeFileName);
            return StatusCode(500, new { message = $"Ingestion failed: {ex.Message}" });
        }

        return Ok(new IngestResponse { FileName = safeFileName, Message = "Document ingested and indexed successfully." });
    }

    [HttpDelete("{fileName}")]
    public async Task<IActionResult> Remove(string fileName, CancellationToken cancellationToken)
    {
        var storePath = Path.GetFullPath(_ragOptions.DocumentStorePath);
        var safeFileName = Path.GetFileName(fileName);
        var filePath = Path.Combine(storePath, safeFileName);

        await _ingestion.RemoveAsync(filePath, cancellationToken);

        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        return Ok(new RemoveResponse { FileName = safeFileName, Message = "Document removed from index and store." });
    }
}
