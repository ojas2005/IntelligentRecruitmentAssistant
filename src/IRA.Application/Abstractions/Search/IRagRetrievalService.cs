using IRA.Domain.Enums;
using IRA.Domain.ValueObjects;

namespace IRA.Application.Abstractions.Search;

/// <summary>
/// Retrieval side of RAG: embeds a query, performs semantic search over the vector
/// store and returns grounding citations for prompt augmentation.
/// </summary>
public interface IRagRetrievalService
{
    Task<IReadOnlyList<Citation>> RetrieveAsync(
        string query,
        int topK = 5,
        DocumentCategory? categoryFilter = null,
        CancellationToken cancellationToken = default);
}
