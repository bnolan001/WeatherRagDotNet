using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WeatherRag.Api.Models;
using WeatherRag.Inference.Options;

namespace WeatherRag.Api.Controllers;

[ApiController]
[Route("api/[controller]s")]
public sealed class ModelController : ControllerBase
{
    private readonly InferenceOptions _options;
    private readonly ILogger<ModelController> _logger;

    public ModelController(IOptions<InferenceOptions> options, ILogger<ModelController> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetModels()
    {
        var models = _options.Models.Select(kvp =>
        {
            var modelPath = Path.GetFullPath(kvp.Value.ModelPath);
            var isAvailable = System.IO.File.Exists(modelPath);

            if (!isAvailable)
            {
                _logger.LogDebug(
                    "Model '{ModelId}' is not available: file not found at {ModelPath}",
                    kvp.Key,
                    modelPath);
            }

            return new ModelInfoDto
            {
                Id = kvp.Key,
                DisplayName = kvp.Value.DisplayName,
                IsAvailable = isAvailable,
                IsDefault = kvp.Key == _options.DefaultModelId
            };
        }).ToList();

        return Ok(models);
    }
}
