using IRA.Api.Auth;
using IRA.Application.Abstractions.Persistence;
using IRA.Application.DTOs;
using IRA.Application.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Api.Controllers;

/// <summary>Interview APIs — retrieve generated interview kits for a job's shortlist.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = RecruitmentPolicies.Recruiters)]
public class InterviewController : ControllerBase
{
    private readonly ITalentRepository _repository;

    public InterviewController(ITalentRepository repository) => _repository = repository;

    [HttpGet("job/{jobId:guid}")]
    public async Task<ActionResult<IEnumerable<InterviewKitDto>>> GetForJob(Guid jobId, CancellationToken ct)
    {
        var kits = await _repository.GetInterviewKitsForJobAsync(jobId, ct);
        return Ok(kits.Select(k => k.ToDto()));
    }
}
