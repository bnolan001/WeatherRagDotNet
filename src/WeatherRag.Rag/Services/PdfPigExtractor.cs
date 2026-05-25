using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using WeatherRag.Rag.Models;

namespace WeatherRag.Rag.Services;

public sealed class PdfPigExtractor : IPdfExtractor
{
    private readonly ILogger<PdfPigExtractor> _logger;

    public PdfPigExtractor(ILogger<PdfPigExtractor> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<(int Page, string Text, IReadOnlyList<ImageReference> Images)> ExtractPagesAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sanitized = Path.GetFullPath(filePath);
        if (!File.Exists(sanitized))
            throw new FileNotFoundException("Source PDF not found.", sanitized);

        using var document = PdfDocument.Open(sanitized);
        int pageCount = document.NumberOfPages;

        for (int pageNum = 1; pageNum <= pageCount; pageNum++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Page page;
            try
            {
                page = document.GetPage(pageNum);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read page {Page} from {File}.", pageNum, sanitized);
                continue;
            }

            var text = page.Text ?? string.Empty;
            var images = ExtractImageReferences(sanitized, pageNum, page);

            yield return (pageNum, text, images);
            await Task.Yield();
        }
    }

    private static IReadOnlyList<ImageReference> ExtractImageReferences(string filePath, int pageNumber, Page page)
    {
        var refs = new List<ImageReference>();
        int idx = 0;
        foreach (var img in page.GetImages())
        {
            refs.Add(new ImageReference
            {
                SourceFile = filePath,
                PageNumber = pageNumber,
                ImageIndex = idx++,
                Width = img.WidthInSamples,
                Height = img.HeightInSamples
            });
        }
        return refs;
    }
}
