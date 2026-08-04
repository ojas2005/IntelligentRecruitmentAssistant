using Azure;
using Azure.AI.OpenAI;
using IRA.Application.Abstractions.AI;
using IRA.Infrastructure.Configuration;
using IRA.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;

namespace IRA.Infrastructure.AI;

/// <summary>
/// Live embedding generator backed by Azure OpenAI embedding models.
/// Wrapped in retry to tolerate transient throttling.
/// </summary>
public class AzureOpenAIEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly EmbeddingClient _client;
    private readonly ILogger<AzureOpenAIEmbeddingGenerator> _logger;

    public AzureOpenAIEmbeddingGenerator(AzureOpenAIOptions options, ILogger<AzureOpenAIEmbeddingGenerator> logger)
    {
        var azureClient = new AzureOpenAIClient(new Uri(options.Endpoint), new AzureKeyCredential(options.ApiKey));
        _client = azureClient.GetEmbeddingClient(options.EmbeddingDeployment);
        _logger = logger;
    }

    public async Task<ReadOnlyMemory<float>> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        return await RetryExecutor.ExecuteAsync(async ct =>
        {
            var response = await _client.GenerateEmbeddingAsync(text, cancellationToken: ct);
            return response.Value.ToFloats();
        }, _logger, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        return await RetryExecutor.ExecuteAsync(async ct =>
        {
            var response = await _client.GenerateEmbeddingsAsync(texts, cancellationToken: ct);
            IReadOnlyList<ReadOnlyMemory<float>> vectors = response.Value.Select(e => e.ToFloats()).ToList();
            return vectors;
        }, _logger, cancellationToken: cancellationToken);
    }
}
