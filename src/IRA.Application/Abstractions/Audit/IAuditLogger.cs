using IRA.Domain.Entities;

namespace IRA.Application.Abstractions.Audit;

/// <summary>Persists an auditable trail of recruiter actions, evaluations and AI activity.</summary>
public interface IAuditLogger
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditEntry>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default);
}
