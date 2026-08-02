using IRA.Application.Abstractions.Audit;
using IRA.Application.Common;
using IRA.Application.DTOs;
using IRA.Application.Mapping;

namespace IRA.Application.Features.Audit;

public record GetAuditTrailQuery(int Count = 100) : IQuery<IReadOnlyList<AuditEntryDto>>;

public class GetAuditTrailQueryHandler : IQueryHandler<GetAuditTrailQuery, IReadOnlyList<AuditEntryDto>>
{
    private readonly IAuditLogger _audit;

    public GetAuditTrailQueryHandler(IAuditLogger audit) => _audit = audit;

    public async Task<IReadOnlyList<AuditEntryDto>> HandleAsync(GetAuditTrailQuery query, CancellationToken ct = default)
    {
        var entries = await _audit.GetRecentAsync(query.Count, ct);
        return entries.Select(e => e.ToDto()).ToList();
    }
}
