using IRA.Domain.Common;

namespace IRA.Domain.Entities;

/// <summary>
/// An immutable audit record of a recruiter action, candidate evaluation, or AI activity.
/// Supports the auditability & traceability acceptance criteria.
/// </summary>
public class AuditEntry : Entity
{
    public string Actor { get; }
    public string Action { get; }
    public string EntityType { get; }
    public string? EntityId { get; }
    public string? Details { get; }

    public AuditEntry(string actor, string action, string entityType, string? entityId = null, string? details = null)
    {
        Actor = actor;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Details = details;
    }
}
