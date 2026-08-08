using IRA.Application.DTOs;
using IRA.Web.Auth;
using IRA.Web.Models;
using IRA.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Web.Controllers;

/// <summary>Ranking Dashboard — run the AI evaluation and view the shortlist.</summary>
[Authorize(Roles = WebRoles.RecruiterPortal)]
public class RankingController : Controller
{
    private readonly RecruitmentApiClient _api;

    public RankingController(RecruitmentApiClient api) => _api = api;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new RankingViewModel();
        try
        {
            vm.Jobs = await _api.GetJobDescriptionsAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = $"Backend API unreachable: {ex.Message}. Start the IRA.Api project.";
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Evaluate(Guid jobId, int shortlistSize, CancellationToken ct)
    {
        var request = new EvaluateCandidatesRequestDto
        {
            JobDescriptionId = jobId,
            InterviewShortlistSize = shortlistSize <= 0 ? 5 : shortlistSize
        };

        var result = await _api.EvaluateAsync(request, ct);
        var vm = new RankingViewModel
        {
            Jobs = await _api.GetJobDescriptionsAsync(ct),
            SelectedJobId = jobId,
            Result = result
        };

        return View("Index", vm);
    }
}
