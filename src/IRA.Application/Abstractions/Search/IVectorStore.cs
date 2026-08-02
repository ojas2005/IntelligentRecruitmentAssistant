using IRA.Domain.Enums;

namespace IRA.Application.Abstractions.Search;

/// <summary>A document/chunk plus its embedding stored in the vector database (Azure AI Search).</summary>
public record VectorRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Content { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
    public DocumentCategory Category { get; init; }
    public ReadOnlyMemory<float> Embedding { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>A hit returned from a semantic search over the vector store.</summary>
public record VectorSearchResult(string Id, string Content, string SourceName, double Score, DocumentCategory Category);

/// <summary>
/// Vector database port (implemented by Azure AI Search, with an in-memory fallback).
/// Provides the retrieval half of Retrieval-Augmented Generation.
/// </summary>
public interface IVectorStore
{
    Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken = default);

    Task UpsertBatchAsync(IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK = 5,
        DocumentCategory? categoryFilter = null,
        CancellationToken cancellationToken = default);
}
