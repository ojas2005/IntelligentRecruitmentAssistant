namespace IRA.Application.Abstractions.AI;

/// <summary>
/// Abstraction over the Azure OpenAI chat/completion model used by the AI agents.
/// The Domain and Application layers never touch the Azure SDK directly — only this port.
/// </summary>
public interface ITextGenerationService
{
    /// <summary>
    /// True when a live Azure OpenAI endpoint is configured. When false the system
    /// transparently uses deterministic fallbacks (fallback-during-AI-disruption requirement).
    /// </summary>
    bool IsLive { get; }

    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}
