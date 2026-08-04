using IRA.Application.Abstractions.AI;
using IRA.Infrastructure.Configuration;
using IRA.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace IRA.Infrastructure.AI;

/// <summary>
/// Text generation via a Semantic Kernel <see cref="Kernel"/> configured with Azure OpenAI
/// chat completion. This is the "Semantic Kernel Implementation" that the AI agents call for
/// generative steps. When Azure OpenAI is not configured, <see cref="IsLive"/> is false and
/// agents transparently switch to their deterministic fallbacks.
/// </summary>
public class SemanticKernelTextGenerationService : ITextGenerationService
{
    private readonly Kernel? _kernel;
    private readonly IChatCompletionService? _chat;
    private readonly ILogger<SemanticKernelTextGenerationService> _logger;

    public SemanticKernelTextGenerationService(AzureOpenAIOptions options, ILogger<SemanticKernelTextGenerationService> logger)
    {
        _logger = logger;

        if (options.IsConfigured)
        {
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: options.ChatDeployment,
                endpoint: options.Endpoint,
                apiKey: options.ApiKey);
            _kernel = builder.Build();
            _chat = _kernel.GetRequiredService<IChatCompletionService>();
        }
    }

    public bool IsLive => _chat is not null;

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        if (_chat is null)
        {
            throw new InvalidOperationException("Azure OpenAI is not configured; the caller should use its fallback path.");
        }

        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(userPrompt);

        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.2,
            MaxTokens = 1500
        };

        return await RetryExecutor.ExecuteAsync(async ct =>
        {
            var response = await _chat.GetChatMessageContentAsync(history, settings, _kernel, ct);
            return response.Content ?? string.Empty;
        }, _logger, cancellationToken: cancellationToken);
    }
}
