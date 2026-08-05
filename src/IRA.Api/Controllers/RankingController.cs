using IRA.Api.Auth;
using IRA.Application.Common;
using IRA.Application.DTOs;
using IRA.Application.Features.Evaluation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Api.Controllers;

/// <summary>Ranking APIs — retrieve the validated candidate shortlist for a job.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = RecruitmentPolicies.Recruiters)]
public class RankingController : ControllerBase
{
    private readonly IQueryHandler<GetRankingQuery, CandidateRankingDto?> _getRanking;

    public RankingController(IQueryHandler<GetRankingQuery, CandidateRankingDto?> getRanking) => _getRanking = getRanking;

    [HttpGet("job/{jobId:guid}")]
    public async Task<ActionResult<CandidateRankingDto>> GetForJob(Guid jobId, CancellationToken ct)
    {
        var ranking = await _getRanking.HandleAsync(new GetRankingQuery(jobId), ct);
        return ranking is null ? NotFound() : Ok(ranking);
    }
}
