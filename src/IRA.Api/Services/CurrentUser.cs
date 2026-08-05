using System.Security.Claims;
using IRA.Api.Auth;

namespace IRA.Api.Services;

/// <summary>Resolves the acting user's identity for audit logging and self-service lookups.</summary>
public interface ICurrentUser
{
    /// <summary>Human/audit-friendly identifier (preferred_username, falling back to name).</summary>
    string Name { get; }

    /// <summary>The login/username, used to look the account up in the user store.</summary>
    string Username { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public string Name =>
        Principal?.FindFirstValue(RecruitmentClaims.PreferredUsername)
        ?? Principal?.Identity?.Name
        ?? "anonymous";

    public string Username =>
        Principal?.FindFirstValue(RecruitmentClaims.PreferredUsername)
        ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.Identity?.Name
        ?? "anonymous";

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
