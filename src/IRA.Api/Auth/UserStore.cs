using System.Collections.Concurrent;

namespace IRA.Api.Auth;

/// <summary>An application account. Candidates may be linked to a parsed candidate profile.</summary>
public class AppUser
{
    public required string Username { get; init; }
    public required string PasswordHash { get; init; }
    public required string DisplayName { get; init; }
    public required string[] Roles { get; init; }
    public string? Email { get; init; }

    /// <summary>Set once a candidate uploads their own resume, linking account → parsed profile.</summary>
    public Guid? CandidateId { get; set; }
}

/// <summary>Account store backing the JWT login flow (async to support durable back-ends).</summary>
public interface IUserStore
{
    Task<AppUser?> FindAsync(string username, CancellationToken ct = default);

    /// <summary>Registers a candidate. Returns the new account, or null if the username is taken.</summary>
    Task<AppUser?> TryRegisterCandidateAsync(string username, string password, string displayName, string? email, CancellationToken ct = default);

    Task LinkCandidateAsync(string username, Guid candidateId, CancellationToken ct = default);
}

/// <summary>Demo credentials seeded into every store back-end. Password: <c>Passw0rd!</c></summary>
public static class DemoUsers
{
    public const string Password = "Passw0rd!";

    public static IEnumerable<AppUser> Seed() => new[]
    {
        Make("recruiter", "Riya Recruiter", "recruiter@dev.local", RecruitmentRoles.Recruiter),
        Make("manager", "Manish Hiring Manager", "manager@dev.local", RecruitmentRoles.HiringManager),
        Make("admin", "Aditi Administrator", "admin@dev.local",
            RecruitmentRoles.Recruiter, RecruitmentRoles.HiringManager, RecruitmentRoles.Administrator),
        Make("candidate", "Chandra Candidate", "candidate@dev.local", RecruitmentRoles.Candidate),
    };

    private static AppUser Make(string username, string displayName, string email, params string[] roles) => new()
    {
        Username = username,
        PasswordHash = PasswordHasher.Hash(Password),
        DisplayName = displayName,
        Email = email,
        Roles = roles
    };
}

/// <summary>
/// In-memory account store seeded with demo credentials so the JWT flow works offline.
/// Used when Cosmos DB is not configured (registered accounts reset on restart).
/// </summary>
public class InMemoryUserStore : IUserStore
{
    private readonly ConcurrentDictionary<string, AppUser> _users = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryUserStore()
    {
        foreach (var user in DemoUsers.Seed())
        {
            _users[user.Username] = user;
        }
    }

    public Task<AppUser?> FindAsync(string username, CancellationToken ct = default) =>
        Task.FromResult(_users.GetValueOrDefault(username));

    public Task<AppUser?> TryRegisterCandidateAsync(string username, string password, string displayName, string? email, CancellationToken ct = default)
    {
        var user = new AppUser
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(password),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName,
            Email = email,
            Roles = new[] { RecruitmentRoles.Candidate }
        };

        return Task.FromResult(_users.TryAdd(username, user) ? user : null);
    }

    public Task LinkCandidateAsync(string username, Guid candidateId, CancellationToken ct = default)
    {
        if (_users.TryGetValue(username, out var user))
        {
            user.CandidateId = candidateId;
        }

        return Task.CompletedTask;
    }
}
