using IRA.Api.Auth;
using IRA.Api.Services;
using IRA.Application.Abstractions.Persistence;
using IRA.Application.Common;
using IRA.Application.DTOs;
using IRA.Application.Features.Candidates;
using IRA.Application.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Api.Controllers;

/// <summary>Candidate APIs — list parsed profiles (recruiters) and self-profile (candidate portal).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CandidateController : ControllerBase
{
    private readonly IQueryHandler<ListCandidatesQuery, IReadOnlyList<CandidateDto>> _list;
    private readonly ITalentRepository _repository;
    private readonly IUserStore _users;
    private readonly ICurrentUser _currentUser;

    public CandidateController(
        IQueryHandler<ListCandidatesQuery, IReadOnlyList<CandidateDto>> list,
        ITalentRepository repository,
        IUserStore users,
        ICurrentUser currentUser)
    {
        _list = list;
        _repository = repository;
        _users = users;
        _currentUser = currentUser;
    }

    /// <summary>List every parsed candidate profile (recruiter portal).</summary>
    [HttpGet]
    [Authorize(Policy = RecruitmentPolicies.Recruiters)]
    public async Task<ActionResult<IReadOnlyList<CandidateDto>>> GetAll(CancellationToken ct)
        => Ok(await _list.HandleAsync(new ListCandidatesQuery(), ct));

    /// <summary>The signed-in candidate's own parsed profile, or 204 if none uploaded yet.</summary>
    [HttpGet("me")]
    [Authorize(Policy = RecruitmentPolicies.CandidatePortal)]
    public async Task<ActionResult<CandidateDto>> Me(CancellationToken ct)
    {
        var candidateId = _users.Find(_currentUser.Username)?.CandidateId;
        if (candidateId is null)
        {
            return NoContent();
        }

        var candidate = await _repository.GetCandidateAsync(candidateId.Value, ct);
        return candidate is null ? NoContent() : Ok(candidate.ToDto());
    }
}
