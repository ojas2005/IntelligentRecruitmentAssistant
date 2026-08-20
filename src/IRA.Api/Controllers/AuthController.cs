using IRA.Api.Auth;
using IRA.Api.Services;
using IRA.Application.Abstractions.Audit;
using IRA.Application.DTOs;
using IRA.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRA.Api.Controllers;

/// <summary>
/// Authentication APIs — self-issued JWT login &amp; candidate registration, plus the
/// identity of the current user.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserStore _users;
    private readonly JwtTokenService _tokens;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;

    public AuthController(IUserStore users, JwtTokenService tokens, ICurrentUser currentUser, IAuditLogger audit)
    {
        _users = users;
        _tokens = tokens;
        _currentUser = currentUser;
        _audit = audit;
    }

    /// <summary>Exchange username/password for a signed JWT bearer token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var user = await _users.FindAsync(request.Username, ct);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        await _audit.LogAsync(new AuditEntry(user.Username, "UserLoggedIn", nameof(AppUser), user.Username,
            $"Roles: {string.Join(", ", user.Roles)}"), ct);

        return Ok(BuildResult(user));
    }

    /// <summary>Self-service candidate sign-up. New accounts get the Candidate role only.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        if (request.Password.Length < 6)
        {
            return BadRequest(new { message = "Password must be at least 6 characters." });
        }

        var user = await _users.TryRegisterCandidateAsync(request.Username, request.Password, request.DisplayName, request.Email, ct);
        if (user is null)
        {
            return Conflict(new { message = "That username is already taken." });
        }

        await _audit.LogAsync(new AuditEntry(user.Username, "CandidateRegistered", nameof(AppUser), user.Username,
            "Self-service candidate sign-up."), ct);

        return Ok(BuildResult(user));
    }

    /// <summary>Returns the authenticated user's identity and roles.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken ct)
    {
        var user = await _users.FindAsync(_currentUser.Username, ct);
        var roles = User.FindAll(RecruitmentClaims.Role).Select(c => c.Value).Distinct().ToList();

        return Ok(new UserProfileDto(
            _currentUser.Username,
            user?.DisplayName ?? _currentUser.Name,
            roles,
            user?.CandidateId));
    }

    private AuthResultDto BuildResult(AppUser user)
    {
        var (token, expires) = _tokens.CreateToken(user);
        return new AuthResultDto(token, user.Username, user.DisplayName, user.Roles, expires, user.CandidateId);
    }
}
