using IRA.Application.Abstractions.AI;
using IRA.Application.Abstractions.Search;
using IRA.Domain.Enums;
using IRA.Domain.ValueObjects;

namespace IRA.Infrastructure.Search;

/// <summary>
/// Retrieval side of RAG: embeds the query, runs semantic search over the vector store
/// and projects hits into grounding <see cref="Citation"/>s used to augment agent prompts
/// and to display supporting reasoning/citations to recruiters.
/// </summary>
public class RagRetrievalService : IRagRetrievalService
{
    private readonly IEmbeddingGenerator _embeddings;
    private readonly IVectorStore _vectorStore;

    public RagRetrievalService(IEmbeddingGenerator embeddings, IVectorStore vectorStore)
    {
        _embeddings = embeddings;
        _vectorStore = vectorStore;
    }

    public async Task<IReadOnlyList<Citation>> RetrieveAsync(
        string query,
        int topK = 5,
        DocumentCategory? categoryFilter = null,
        CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddings.GenerateAsync(query, cancellationToken);
        var hits = await _vectorStore.SearchAsync(embedding, topK, categoryFilter, cancellationToken);

        return hits.Select(h => new Citation(
            h.Id,
            h.SourceName,
            Snippet(h.Content),
            Math.Round(h.Score, 4))).ToList();
    }

    private static string Snippet(string content)
    {
        content = content.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return content.Length <= 280 ? content : content[..280] + "…";
    }
}
