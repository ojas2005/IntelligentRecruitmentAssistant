using IRA.Application.Abstractions.AI;

namespace IRA.Infrastructure.AI;

/// <summary>
/// Offline fallback embedding generator. Produces a fixed-dimension, L2-normalised
/// bag-of-tokens vector so that cosine similarity remains meaningful for RAG retrieval
/// even when Azure OpenAI is not configured. Deterministic: identical input -> identical vector.
/// </summary>
public class DeterministicEmbeddingGenerator : IEmbeddingGenerator
{
    private const int Dimensions = 384;

    public Task<ReadOnlyMemory<float>> GenerateAsync(string text, CancellationToken cancellationToken = default) =>
        Task.FromResult(Embed(text));

    public Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ReadOnlyMemory<float>> result = texts.Select(Embed).ToList();
        return Task.FromResult(result);
    }

    private static ReadOnlyMemory<float> Embed(string text)
    {
        var vector = new float[Dimensions];
        if (string.IsNullOrWhiteSpace(text))
        {
            return vector;
        }

        var tokens = text.ToLowerInvariant()
            .Split(new[] { ' ', '\n', '\r', '\t', ',', '.', ';', ':', '/', '\\', '(', ')', '-', '|' },
                StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            var bucket = (int)(StableHash(token) % Dimensions);
            vector[bucket] += 1f;
        }

        // L2 normalise.
        var magnitude = MathF.Sqrt(vector.Sum(v => v * v));
        if (magnitude > 0)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= magnitude;
            }
        }

        return vector;
    }

    private static uint StableHash(string value)
    {
        // FNV-1a — stable across runs and platforms (unlike string.GetHashCode).
        const uint offset = 2166136261;
        const uint prime = 16777619;
        uint hash = offset;
        foreach (var c in value)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash;
    }
}
