using IRA.Api.Auth;
using IRA.Api.Services;
using IRA.Application.Common;
using IRA.Application.DTOs;
using IRA.Application.Features.JobDescriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Api.Controllers;

/// <summary>Job Description APIs — register and list roles to match candidates against.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobDescriptionController : ControllerBase
{
    private readonly ICommandHandler<CreateJobDescriptionCommand, JobDescriptionDto> _create;
    private readonly IQueryHandler<ListJobDescriptionsQuery, IReadOnlyList<JobDescriptionDto>> _list;
    private readonly ICurrentUser _currentUser;

    public JobDescriptionController(
        ICommandHandler<CreateJobDescriptionCommand, JobDescriptionDto> create,
        IQueryHandler<ListJobDescriptionsQuery, IReadOnlyList<JobDescriptionDto>> list,
        ICurrentUser currentUser)
    {
        _create = create;
        _list = list;
        _currentUser = currentUser;
    }

    /// <summary>Register a new job description (recruiter portal).</summary>
    [HttpPost]
    [Authorize(Policy = RecruitmentPolicies.Recruiters)]
    public async Task<ActionResult<JobDescriptionDto>> Create([FromBody] CreateJobDescriptionDto dto, CancellationToken ct)
    {
        var result = await _create.HandleAsync(new CreateJobDescriptionCommand(dto, _currentUser.Name), ct);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    /// <summary>List open roles. Candidates may browse; recruiters manage.</summary>
    [HttpGet]
    [Authorize(Policy = RecruitmentPolicies.CandidatePortal)]
    public async Task<ActionResult<IReadOnlyList<JobDescriptionDto>>> GetAll(CancellationToken ct)
        => Ok(await _list.HandleAsync(new ListJobDescriptionsQuery(), ct));
}
