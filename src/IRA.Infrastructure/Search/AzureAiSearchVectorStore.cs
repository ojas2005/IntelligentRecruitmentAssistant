using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using IRA.Application.Abstractions.Search;
using IRA.Domain.Enums;
using IRA.Infrastructure.Configuration;
using IRA.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;

namespace IRA.Infrastructure.Search;

/// <summary>
/// Live vector database backed by Azure AI Search. Creates/updates the vector index on
/// first use and performs k-NN vector search for Retrieval-Augmented Generation.
/// </summary>
public class AzureAiSearchVectorStore : IVectorStore
{
    private const string VectorProfile = "vector-profile";
    private const string VectorAlgorithm = "hnsw-config";

    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private readonly int _dimensions;
    private readonly ILogger<AzureAiSearchVectorStore> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public AzureAiSearchVectorStore(
        AzureSearchOptions options,
        int embeddingDimensions,
        ILogger<AzureAiSearchVectorStore> logger)
    {
        var credential = new AzureKeyCredential(options.ApiKey);
        _indexClient = new SearchIndexClient(new Uri(options.Endpoint), credential);
        _searchClient = _indexClient.GetSearchClient(options.IndexName);
        _dimensions = embeddingDimensions;
        _logger = logger;
    }

    public async Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken = default) =>
        await UpsertBatchAsync(new[] { record }, cancellationToken);

    public async Task UpsertBatchAsync(IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken = default)
    {
        await EnsureIndexAsync(cancellationToken);
        var docs = records.Select(SearchDocumentModel.From).ToList();
        await RetryExecutor.ExecuteAsync(async ct =>
        {
            await _searchClient.MergeOrUploadDocumentsAsync(docs, cancellationToken: ct);
            return true;
        }, _logger, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK = 5,
        DocumentCategory? categoryFilter = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureIndexAsync(cancellationToken);

        var vectorQuery = new VectorizedQuery(queryEmbedding) { KNearestNeighborsCount = topK };
        vectorQuery.Fields.Add(nameof(SearchDocumentModel.Embedding).ToLowerInvariant());

        var options = new SearchOptions { Size = topK };
        options.VectorSearch = new VectorSearchOptions { Queries = { vectorQuery } };
        if (categoryFilter is not null)
        {
            options.Filter = $"category eq '{categoryFilter}'";
        }

        return await RetryExecutor.ExecuteAsync(async ct =>
        {
            var response = await _searchClient.SearchAsync<SearchDocumentModel>(searchText: null, options, ct);
            var results = new List<VectorSearchResult>();
            await foreach (var hit in response.Value.GetResultsAsync())
            {
                var d = hit.Document;
                var category = Enum.TryParse<DocumentCategory>(d.Category, out var c) ? c : DocumentCategory.CandidateResume;
                results.Add(new VectorSearchResult(d.Id, d.Content, d.SourceName, hit.Score ?? 0, category));
            }

            return (IReadOnlyList<VectorSearchResult>)results;
        }, _logger, cancellationToken: cancellationToken);
    }

    private async Task EnsureIndexAsync(CancellationToken ct)
    {
        if (_initialized)
        {
            return;
        }

        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialized)
            {
                return;
            }

            var index = new SearchIndex(_searchClient.IndexName)
            {
                Fields =
                {
                    new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                    new SearchableField("content"),
                    new SimpleField("sourceName", SearchFieldDataType.String) { IsFilterable = true },
                    new SimpleField("category", SearchFieldDataType.String) { IsFilterable = true },
                    new SimpleField("metadata", SearchFieldDataType.String),
                    new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = _dimensions,
                        VectorSearchProfileName = VectorProfile
                    }
                },
                VectorSearch = new VectorSearch
                {
                    Profiles = { new VectorSearchProfile(VectorProfile, VectorAlgorithm) },
                    Algorithms = { new HnswAlgorithmConfiguration(VectorAlgorithm) }
                }
            };

            await _indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: ct);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>Serialization model matching the Azure AI Search index schema.</summary>
    private class SearchDocumentModel
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
        [JsonPropertyName("sourceName")] public string SourceName { get; set; } = string.Empty;
        [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;
        [JsonPropertyName("metadata")] public string Metadata { get; set; } = string.Empty;
        [JsonPropertyName("embedding")] public IReadOnlyList<float> Embedding { get; set; } = Array.Empty<float>();

        public static SearchDocumentModel From(VectorRecord record) => new()
        {
            Id = record.Id,
            Content = record.Content,
            SourceName = record.SourceName,
            Category = record.Category.ToString(),
            Metadata = JsonSerializer.Serialize(record.Metadata),
            Embedding = record.Embedding.ToArray()
        };
    }
}
