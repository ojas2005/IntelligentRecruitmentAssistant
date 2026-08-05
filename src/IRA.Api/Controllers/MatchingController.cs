using IRA.Api.Auth;
using IRA.Api.Services;
using IRA.Application.Abstractions.Persistence;
using IRA.Application.Common;
using IRA.Application.DTOs;
using IRA.Application.Features.Evaluation;
using IRA.Application.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Api.Controllers;

/// <summary>
/// Matching APIs — trigger the full AI evaluation workflow
/// (Extract → Analyze → Match → Generate Questions → Rank) and read back evaluations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = RecruitmentPolicies.Recruiters)]
public class MatchingController : ControllerBase
{
    private readonly ICommandHandler<EvaluateCandidatesCommand, RecruitmentEvaluationResultDto> _evaluate;
    private readonly ITalentRepository _repository;
    private readonly ICurrentUser _currentUser;

    public MatchingController(
        ICommandHandler<EvaluateCandidatesCommand, RecruitmentEvaluationResultDto> evaluate,
        ITalentRepository repository,
        ICurrentUser currentUser)
    {
        _evaluate = evaluate;
        _repository = repository;
        _currentUser = currentUser;
    }

    /// <summary>Runs the orchestrated multi-agent evaluation for a job description.</summary>
    [HttpPost("evaluate")]
    public async Task<ActionResult<RecruitmentEvaluationResultDto>> Evaluate(
        [FromBody] EvaluateCandidatesRequestDto request,
        CancellationToken ct)
    {
        var result = await _evaluate.HandleAsync(new EvaluateCandidatesCommand(request, _currentUser.Name), ct);
        return Ok(result);
    }

    /// <summary>Returns the stored evaluations for a job.</summary>
    [HttpGet("job/{jobId:guid}/evaluations")]
    public async Task<ActionResult<IEnumerable<CandidateEvaluationDto>>> GetEvaluations(Guid jobId, CancellationToken ct)
    {
        var evaluations = await _repository.GetEvaluationsForJobAsync(jobId, ct);
        return Ok(evaluations.Select(e => e.ToDto()));
    }
}
