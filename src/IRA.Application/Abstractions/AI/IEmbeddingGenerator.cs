namespace IRA.Application.Abstractions.AI;

/// <summary>Generates vector embeddings via Azure OpenAI embedding models.</summary>
public interface IEmbeddingGenerator
{
    Task<ReadOnlyMemory<float>> GenerateAsync(string text, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}
