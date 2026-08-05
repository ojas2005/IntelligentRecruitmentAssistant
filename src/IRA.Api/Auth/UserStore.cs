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

/// <summary>Account store backing the JWT login flow.</summary>
public interface IUserStore
{
    AppUser? Find(string username);
    bool TryRegisterCandidate(string username, string password, string displayName, string? email, out AppUser? user);
    void LinkCandidate(string username, Guid candidateId);
}

/// <summary>
/// In-memory account store seeded with demo credentials so the JWT flow works offline,
/// mirroring the offline-fallback approach used across the rest of the solution.
/// Demo password for every seeded account: <c>Passw0rd!</c>
/// </summary>
public class InMemoryUserStore : IUserStore
{
    private readonly ConcurrentDictionary<string, AppUser> _users = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryUserStore()
    {
        const string demoPassword = "Passw0rd!";
        Seed("recruiter", demoPassword, "Riya Recruiter", "recruiter@dev.local", RecruitmentRoles.Recruiter);
        Seed("manager", demoPassword, "Manish Hiring Manager", "manager@dev.local", RecruitmentRoles.HiringManager);
        Seed("admin", demoPassword, "Aditi Administrator", "admin@dev.local",
            RecruitmentRoles.Recruiter, RecruitmentRoles.HiringManager, RecruitmentRoles.Administrator);
        Seed("candidate", demoPassword, "Chandra Candidate", "candidate@dev.local", RecruitmentRoles.Candidate);
    }

    public AppUser? Find(string username) =>
        _users.TryGetValue(username, out var user) ? user : null;

    public bool TryRegisterCandidate(string username, string password, string displayName, string? email, out AppUser? user)
    {
        user = new AppUser
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(password),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName,
            Email = email,
            Roles = new[] { RecruitmentRoles.Candidate }
        };

        return _users.TryAdd(username, user);
    }

    public void LinkCandidate(string username, Guid candidateId)
    {
        if (_users.TryGetValue(username, out var user))
        {
            user.CandidateId = candidateId;
        }
    }

    private void Seed(string username, string password, string displayName, string email, params string[] roles) =>
        _users[username] = new AppUser
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(password),
            DisplayName = displayName,
            Email = email,
            Roles = roles
        };
}
