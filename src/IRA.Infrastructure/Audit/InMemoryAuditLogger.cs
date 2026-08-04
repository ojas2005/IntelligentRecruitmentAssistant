using System.Collections.Concurrent;
using IRA.Application.Abstractions.Audit;
using IRA.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IRA.Infrastructure.Audit;

/// <summary>
/// Captures the audit trail in memory and mirrors every entry to the logging pipeline
/// (which flows to Application Insights when configured). Provides auditability & monitoring.
/// </summary>
public class InMemoryAuditLogger : IAuditLogger
{
    private readonly ConcurrentQueue<AuditEntry> _entries = new();
    private readonly ILogger<InMemoryAuditLogger> _logger;

    public InMemoryAuditLogger(ILogger<InMemoryAuditLogger> logger) => _logger = logger;

    public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        _entries.Enqueue(entry);
        _logger.LogInformation("AUDIT {Actor} {Action} {EntityType}#{EntityId} {Details}",
            entry.Actor, entry.Action, entry.EntityType, entry.EntityId, entry.Details);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditEntry>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AuditEntry> recent = _entries
            .Reverse()
            .Take(count)
            .ToList();
        return Task.FromResult(recent);
    }
}
