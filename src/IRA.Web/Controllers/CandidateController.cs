using IRA.Application.DTOs;
using IRA.Web.Auth;
using IRA.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Web.Controllers;

/// <summary>Candidate Dashboard (recruiter portal — lists all candidates).</summary>
[Authorize(Roles = WebRoles.RecruiterPortal)]
public class CandidateController : Controller
{
    private readonly RecruitmentApiClient _api;

    public CandidateController(RecruitmentApiClient api) => _api = api;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            return View(await _api.GetCandidatesAsync(ct));
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = $"Backend API unreachable: {ex.Message}. Start the IRA.Api project.";
            return View(Array.Empty<CandidateDto>());
        }
    }
}
