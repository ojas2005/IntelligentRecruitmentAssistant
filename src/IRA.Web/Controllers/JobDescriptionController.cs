using IRA.Application.DTOs;
using IRA.Web.Auth;
using IRA.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Web.Controllers;

/// <summary>Job Description Management Screens (recruiter portal). Candidates browse via /Portal/Jobs.</summary>
[Authorize(Roles = WebRoles.RecruiterPortal)]
public class JobDescriptionController : Controller
{
    private readonly RecruitmentApiClient _api;

    public JobDescriptionController(RecruitmentApiClient api) => _api = api;

    public async Task<IActionResult> Index(CancellationToken ct)
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

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string title,
        string? department,
        string rawText,
        double minYearsExperience,
        string? requiredSkills,
        string? preferredSkills,
        CancellationToken ct)
    {
        var dto = new CreateJobDescriptionDto
        {
            Title = title,
            Department = department,
            RawText = rawText,
            MinYearsExperience = minYearsExperience,
            RequiredSkills = SplitSkills(requiredSkills),
            PreferredSkills = SplitSkills(preferredSkills)
        };

        await _api.CreateJobDescriptionAsync(dto, ct);
        TempData["Message"] = $"Job description '{title}' created.";
        return RedirectToAction(nameof(Index));
    }

    private static List<string> SplitSkills(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? new List<string>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
