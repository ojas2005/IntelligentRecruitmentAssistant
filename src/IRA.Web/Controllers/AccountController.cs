using System.Security.Claims;
using IRA.Application.DTOs;
using IRA.Web.Auth;
using IRA.Web.Models;
using IRA.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Web.Controllers;

/// <summary>Sign-in, candidate self-registration and sign-out for both portals.</summary>
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly RecruitmentApiClient _api;

    public AccountController(RecruitmentApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLanding();
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        AuthResultDto? auth;
        try
        {
            auth = await _api.LoginAsync(new LoginRequestDto(model.Username, model.Password), ct);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Backend API unreachable. Start the IRA.Api project.");
            return View(model);
        }

        if (auth is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        await SignInAsync(auth);
        return RedirectAfterAuth(auth, model.ReturnUrl);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLanding();
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        (AuthResultDto? Result, string? Error) outcome;
        try
        {
            outcome = await _api.RegisterAsync(
                new RegisterRequestDto(model.Username, model.Password, model.DisplayName, model.Email), ct);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Backend API unreachable. Start the IRA.Api project.");
            return View(model);
        }

        if (outcome.Result is null)
        {
            ModelState.AddModelError(string.Empty, outcome.Error ?? "Registration failed.");
            return View(model);
        }

        await SignInAsync(outcome.Result);
        return RedirectToAction("Index", "Portal");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(AuthResultDto auth)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, auth.DisplayName),
            new("username", auth.Username),
            new(RecruitmentApiClient.AccessTokenClaim, auth.Token)
        };

        claims.AddRange(auth.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        if (auth.CandidateId is { } candidateId)
        {
            claims.Add(new Claim("candidate_id", candidateId.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = auth.ExpiresAtUtc
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), properties);
    }

    private IActionResult RedirectAfterAuth(AuthResultDto auth, string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return IsRecruiter(auth.Roles)
            ? RedirectToAction("Index", "Home")
            : RedirectToAction("Index", "Portal");
    }

    private IActionResult RedirectToLanding() =>
        User.IsInRole(WebRoles.Recruiter) || User.IsInRole(WebRoles.HiringManager) || User.IsInRole(WebRoles.Administrator)
            ? RedirectToAction("Index", "Home")
            : RedirectToAction("Index", "Portal");

    private static bool IsRecruiter(IEnumerable<string> roles) =>
        roles.Any(r => r is WebRoles.Recruiter or WebRoles.HiringManager or WebRoles.Administrator);
}
