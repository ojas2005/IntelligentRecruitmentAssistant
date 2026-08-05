using IRA.Api.Auth;
using IRA.Api.Services;
using IRA.Application.DTOs;
using IRA.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Api.Controllers;

/// <summary>Resume APIs — upload (single &amp; bulk) and processing status.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResumeController : ControllerBase
{
    private readonly ResumeProcessingService _processing;
    private readonly ICurrentUser _currentUser;
    private readonly IUserStore _users;

    public ResumeController(ResumeProcessingService processing, ICurrentUser currentUser, IUserStore users)
    {
        _processing = processing;
        _currentUser = currentUser;
        _users = users;
    }

    /// <summary>
    /// Upload and process a single resume. Available to recruiters and to candidates
    /// uploading their own resume — a candidate upload links the resulting profile to the account.
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Policy = RecruitmentPolicies.CandidatePortal)]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ResumeUploadResultDto>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("A non-empty resume file is required.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _processing.ProcessAsync(stream, file.FileName, _currentUser.Name, ct);

        // A candidate uploading their own resume owns the parsed profile.
        if (_currentUser.IsInRole(RecruitmentRoles.Candidate))
        {
            _users.LinkCandidate(_currentUser.Username, result.CandidateId);
        }

        return Ok(result);
    }

    /// <summary>Bulk resume upload — processes each resume and reports per-file results.</summary>
    [HttpPost("bulk-upload")]
    [Authorize(Policy = RecruitmentPolicies.Recruiters)]
    [RequestSizeLimit(200_000_000)]
    public async Task<ActionResult<IEnumerable<ResumeUploadResultDto>>> BulkUpload(
        [FromForm] IFormFileCollection files,
        CancellationToken ct)
    {
        if (files is null || files.Count == 0)
        {
            return BadRequest("At least one resume file is required.");
        }

        var results = new List<ResumeUploadResultDto>();
        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            results.Add(await _processing.ProcessAsync(stream, file.FileName, _currentUser.Name, ct));
        }

        return Ok(results);
    }
}
