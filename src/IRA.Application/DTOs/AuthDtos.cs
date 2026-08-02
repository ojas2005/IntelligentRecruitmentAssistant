namespace IRA.Application.DTOs;

/// <summary>Credentials submitted to the login endpoint.</summary>
public record LoginRequestDto(string Username, string Password);

/// <summary>Candidate self-service sign-up.</summary>
public record RegisterRequestDto(string Username, string Password, string DisplayName, string? Email);

/// <summary>Result of a successful login/registration — carries the bearer token.</summary>
public record AuthResultDto(
    string Token,
    string Username,
    string DisplayName,
    IReadOnlyList<string> Roles,
    DateTime ExpiresAtUtc,
    Guid? CandidateId);

/// <summary>The current authenticated user's identity (from <c>/api/auth/me</c>).</summary>
public record UserProfileDto(
    string Username,
    string DisplayName,
    IReadOnlyList<string> Roles,
    Guid? CandidateId);
