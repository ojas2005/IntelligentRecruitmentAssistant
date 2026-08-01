namespace IRA.Domain.ValueObjects;

/// <summary>
/// A traceable reference back to a source document that grounded an AI recommendation.
/// Supports the "display supporting reasoning and citations" requirement (RAG traceability).
/// </summary>
/// <param name="SourceId">Identifier of the source chunk/document in the vector store.</param>
/// <param name="SourceName">Human-readable document name (e.g. "Software Developer JD").</param>
/// <param name="Snippet">The excerpt that supports the statement.</param>
/// <param name="Score">Relevance score of the retrieved chunk.</param>
public readonly record struct Citation(string SourceId, string SourceName, string Snippet, double Score);
