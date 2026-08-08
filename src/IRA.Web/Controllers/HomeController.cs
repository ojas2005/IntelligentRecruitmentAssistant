using IRA.Web.Auth;
using IRA.Web.Models;
using IRA.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Web.Controllers;

/// <summary>Recruiter Dashboard and Recruitment Analytics screens.</summary>
[Authorize]
public class HomeController : Controller
{
    private readonly RecruitmentApiClient _api;

    public HomeController(RecruitmentApiClient api) => _api = api;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Candidates get their own portal; the dashboard is a recruiter surface.
        if (User.IsInRole(WebRoles.Candidate)
            && !(User.IsInRole(WebRoles.Recruiter) || User.IsInRole(WebRoles.HiringManager) || User.IsInRole(WebRoles.Administrator)))
        {
            return RedirectToAction("Index", "Portal");
        }

        var vm = new DashboardViewModel();
        try
        {
            vm.Candidates = await _api.GetCandidatesAsync(ct);
            vm.Jobs = await _api.GetJobDescriptionsAsync(ct);
            vm.ApiReachable = true;
        }
        catch (Exception ex)
        {
            vm.ApiReachable = false;
            vm.Error = ex.Message;
        }

        return View(vm);
    }

    [Authorize(Roles = WebRoles.RecruiterPortal)]
    public async Task<IActionResult> Analytics(CancellationToken ct)
    {
        var audit = await _api.GetAuditAsync(100, ct);
        return View(audit);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
