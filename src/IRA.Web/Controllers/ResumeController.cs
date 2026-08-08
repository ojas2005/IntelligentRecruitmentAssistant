using IRA.Web.Auth;
using IRA.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Web.Controllers;

/// <summary>
/// Resume Upload Portal. Recruiters upload on behalf of candidates; candidates upload their
/// own resume (which links the parsed profile to their account).
/// </summary>
[Authorize]
public class ResumeController : Controller
{
    private readonly RecruitmentApiClient _api;

    public ResumeController(RecruitmentApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.IsCandidate = User.IsInRole(WebRoles.Candidate)
            && !(User.IsInRole(WebRoles.Recruiter) || User.IsInRole(WebRoles.HiringManager) || User.IsInRole(WebRoles.Administrator));
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(List<IFormFile> files, CancellationToken ct)
    {
        var isCandidate = User.IsInRole(WebRoles.Candidate)
            && !(User.IsInRole(WebRoles.Recruiter) || User.IsInRole(WebRoles.HiringManager) || User.IsInRole(WebRoles.Administrator));

        if (files is null || files.Count == 0)
        {
            TempData["Error"] = "Please select at least one resume file.";
            return RedirectToAction(nameof(Index));
        }

        // A candidate account owns a single profile — only their first file is used.
        var toProcess = isCandidate ? files.Where(f => f.Length > 0).Take(1) : files.Where(f => f.Length > 0);

        var uploaded = 0;
        foreach (var file in toProcess)
        {
            await using var stream = file.OpenReadStream();
            await _api.UploadResumeAsync(stream, file.FileName, ct);
            uploaded++;
        }

        if (isCandidate)
        {
            TempData["Message"] = "Your resume was processed. Here is your profile.";
            return RedirectToAction("Profile", "Portal");
        }

        TempData["Message"] = $"Successfully processed {uploaded} resume(s).";
        return RedirectToAction("Index", "Candidate");
    }
}
