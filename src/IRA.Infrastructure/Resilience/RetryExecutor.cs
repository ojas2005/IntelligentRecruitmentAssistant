using Microsoft.Extensions.Logging;

namespace IRA.Infrastructure.Resilience;

/// <summary>
/// Minimal exponential-backoff retry used to wrap transient Azure service calls,
/// satisfying the "Security and Retry Policies" infrastructure requirement without
/// pulling a heavyweight policy engine into every call site.
/// </summary>
public static class RetryExecutor
{
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        ILogger logger,
        int maxAttempts = 3,
        CancellationToken cancellationToken = default)
    {
        var delay = TimeSpan.FromMilliseconds(400);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (Exception ex) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Transient failure on attempt {Attempt}/{Max}. Retrying in {Delay}ms.",
                    attempt, maxAttempts, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
                delay *= 2;
            }
        }
    }
}
