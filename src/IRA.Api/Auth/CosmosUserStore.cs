using System.Net;
using IRA.Infrastructure.Persistence.Cosmos;
using Microsoft.Azure.Cosmos;

namespace IRA.Api.Auth;

/// <summary>Cosmos document form of an <see cref="AppUser"/> (id == username).</summary>
public sealed class UserDocument
{
    public string Id { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public string? Email { get; set; }
    public Guid? CandidateId { get; set; }

    public static UserDocument From(AppUser u) => new()
    {
        Id = u.Username,
        PasswordHash = u.PasswordHash,
        DisplayName = u.DisplayName,
        Roles = u.Roles,
        Email = u.Email,
        CandidateId = u.CandidateId
    };

    public AppUser ToDomain() => new()
    {
        Username = Id,
        PasswordHash = PasswordHash,
        DisplayName = DisplayName,
        Roles = Roles,
        Email = Email,
        CandidateId = CandidateId
    };
}

/// <summary>
/// Durable account store in Azure Cosmos DB. Registered candidates and profile links survive
/// API restarts. Demo credentials are seeded once on first use so the sample logins keep working.
/// </summary>
public sealed class CosmosUserStore : IUserStore
{
    private readonly CosmosContext _context;
    private readonly SemaphoreSlim _seedLock = new(1, 1);
    private bool _seeded;

    public CosmosUserStore(CosmosContext context) => _context = context;

    public async Task<AppUser?> FindAsync(string username, CancellationToken ct = default)
    {
        var container = await EnsureSeededAsync(ct);
        try
        {
            var response = await container.ReadItemAsync<UserDocument>(username, new PartitionKey(username), cancellationToken: ct);
            return response.Resource.ToDomain();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<AppUser?> TryRegisterCandidateAsync(string username, string password, string displayName, string? email, CancellationToken ct = default)
    {
        var container = await EnsureSeededAsync(ct);
        var user = new AppUser
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(password),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName,
            Email = email,
            Roles = new[] { RecruitmentRoles.Candidate }
        };

        try
        {
            await container.CreateItemAsync(UserDocument.From(user), new PartitionKey(username), cancellationToken: ct);
            return user;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            return null; // username already taken
        }
    }

    public async Task LinkCandidateAsync(string username, Guid candidateId, CancellationToken ct = default)
    {
        var container = await EnsureSeededAsync(ct);
        var existing = await FindAsync(username, ct);
        if (existing is null)
        {
            return;
        }

        existing.CandidateId = candidateId;
        await container.UpsertItemAsync(UserDocument.From(existing), new PartitionKey(username), cancellationToken: ct);
    }

    private async Task<Container> EnsureSeededAsync(CancellationToken ct)
    {
        var container = await _context.GetContainerAsync(CosmosContext.Users, ct);
        if (_seeded)
        {
            return container;
        }

        await _seedLock.WaitAsync(ct);
        try
        {
            if (!_seeded)
            {
                foreach (var user in DemoUsers.Seed())
                {
                    try
                    {
                        await container.CreateItemAsync(UserDocument.From(user), new PartitionKey(user.Username), cancellationToken: ct);
                    }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
                    {
                        // Already seeded on a previous run — leave the existing account untouched.
                    }
                }

                _seeded = true;
            }
        }
        finally
        {
            _seedLock.Release();
        }

        return container;
    }
}
