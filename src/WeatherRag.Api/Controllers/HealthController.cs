using Microsoft.AspNetCore.Mvc;
using WeatherRag.Api.Models;
using WeatherRag.Rag.Services;

namespace WeatherRag.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    private readonly IVectorStore _vectorStore;

    public HealthController(IVectorStore vectorStore)
    {
        _vectorStore = vectorStore;
    }

    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "operational", chunksIndexed = _vectorStore.Count });
    }

    [HttpGet("index")]
    public IActionResult GetIndexStatus([FromServices] Microsoft.Extensions.Options.IOptions<WeatherRag.Rag.Options.VectorStoreOptions> opts)
    {
        return Ok(new IndexStatusResponse
        {
            ChunkCount = _vectorStore.Count,
            StorePath = opts.Value.PersistencePath
        });
    }
}
