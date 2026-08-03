using Azure;
using Azure.AI.DocumentIntelligence;
using IRA.Application.Abstractions.Documents;
using IRA.Infrastructure.Configuration;
using IRA.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;

namespace IRA.Infrastructure.Documents;

/// <summary>
/// Live resume/document extraction using Azure Document Intelligence (prebuilt-read model).
/// </summary>
public class AzureDocumentIntelligenceExtractor : IDocumentExtractor
{
    private readonly DocumentIntelligenceClient _client;
    private readonly ILogger<AzureDocumentIntelligenceExtractor> _logger;

    public AzureDocumentIntelligenceExtractor(
        DocumentIntelligenceOptions options,
        ILogger<AzureDocumentIntelligenceExtractor> logger)
    {
        _client = new DocumentIntelligenceClient(new Uri(options.Endpoint), new AzureKeyCredential(options.ApiKey));
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(Stream document, string fileName, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await document.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        return await RetryExecutor.ExecuteAsync(async ct =>
        {
            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-read",
                BinaryData.FromStream(buffer),
                cancellationToken: ct);

            return operation.Value.Content ?? string.Empty;
        }, _logger, cancellationToken: cancellationToken);
    }
}
