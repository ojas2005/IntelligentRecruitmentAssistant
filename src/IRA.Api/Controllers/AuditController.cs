using IRA.Api.Auth;
using IRA.Application.Common;
using IRA.Application.DTOs;
using IRA.Application.Features.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Api.Controllers;

/// <summary>Audit APIs — the traceable trail of recruiter actions and AI activity (admins only).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = RecruitmentPolicies.Administrators)]
public class AuditController : ControllerBase
{
    private readonly IQueryHandler<GetAuditTrailQuery, IReadOnlyList<AuditEntryDto>> _audit;

    public AuditController(IQueryHandler<GetAuditTrailQuery, IReadOnlyList<AuditEntryDto>> audit) => _audit = audit;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditEntryDto>>> Get([FromQuery] int count = 100, CancellationToken ct = default)
        => Ok(await _audit.HandleAsync(new GetAuditTrailQuery(count), ct));
}
