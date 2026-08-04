using System.Collections.Concurrent;
using IRA.Application.Abstractions.Search;
using IRA.Domain.Enums;

namespace IRA.Infrastructure.Search;

/// <summary>
/// Offline fallback vector database. Holds records in memory and performs brute-force
/// cosine-similarity search — sufficient for development, demos and RAG tests without Azure AI Search.
/// </summary>
public class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<string, VectorRecord> _records = new();

    public Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken = default)
    {
        _records[record.Id] = record;
        return Task.CompletedTask;
    }

    public Task UpsertBatchAsync(IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken = default)
    {
        foreach (var record in records)
        {
            _records[record.Id] = record;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK = 5,
        DocumentCategory? categoryFilter = null,
        CancellationToken cancellationToken = default)
    {
        var candidates = _records.Values
            .Where(r => categoryFilter is null || r.Category == categoryFilter)
            .Select(r => new VectorSearchResult(
                r.Id, r.Content, r.SourceName, CosineSimilarity(queryEmbedding, r.Embedding), r.Category))
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(candidates);
    }

    private static double CosineSimilarity(ReadOnlyMemory<float> aMem, ReadOnlyMemory<float> bMem)
    {
        var a = aMem.Span;
        var b = bMem.Span;
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
        {
            return 0;
        }

        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0 || magB == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
