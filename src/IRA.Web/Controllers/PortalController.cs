using IRA.Application.DTOs;
using IRA.Web.Auth;
using IRA.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Web.Controllers;

/// <summary>
/// Candidate ("user") portal — self-service functionality: landing page, own parsed profile,
/// and read-only browsing of open roles. Recruiters use the recruiter portal instead.
/// </summary>
[Authorize(Roles = WebRoles.Candidate)]
public class PortalController : Controller
{
    private readonly RecruitmentApiClient _api;

    public PortalController(RecruitmentApiClient api) => _api = api;

    public IActionResult Index() => View();

    /// <summary>The candidate's own parsed profile (skills, experience), or a prompt to upload.</summary>
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        try
        {
            return View(await _api.GetMyProfileAsync(ct));
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = $"Backend API unreachable: {ex.Message}. Start the IRA.Api project.";
            return View((CandidateDto?)null);
        }
    }

    /// <summary>Read-only list of open roles a candidate can review.</summary>
    public async Task<IActionResult> Jobs(CancellationToken ct)
    {
        try
        {
            return View(await _api.GetJobDescriptionsAsync(ct));
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = $"Backend API unreachable: {ex.Message}. Start the IRA.Api project.";
            return View(Array.Empty<JobDescriptionDto>());
        }
    }
}
