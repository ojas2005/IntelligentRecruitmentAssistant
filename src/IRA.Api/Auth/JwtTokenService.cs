using System.Security.Claims;
using System.Text;
using IRA.Api.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IRA.Api.Auth;

/// <summary>Claim types used by the self-issued tokens (kept short and stable).</summary>
public static class RecruitmentClaims
{
    public const string Role = "role";
    public const string Name = "name";
    public const string PreferredUsername = "preferred_username";
    public const string CandidateId = "candidate_id";
}

/// <summary>Issues signed JWT access tokens for authenticated accounts.</summary>
public class JwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken(AppUser user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(RecruitmentClaims.PreferredUsername, user.Username),
            new(RecruitmentClaims.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        }

        if (user.CandidateId is { } candidateId)
        {
            claims.Add(new Claim(RecruitmentClaims.CandidateId, candidateId.ToString()));
        }

        claims.AddRange(user.Roles.Select(r => new Claim(RecruitmentClaims.Role, r)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expires);
    }

    /// <summary>Validation parameters matching the tokens this service issues.</summary>
    public TokenValidationParameters ValidationParameters => new()
    {
        ValidateIssuer = true,
        ValidIssuer = _options.Issuer,
        ValidateAudience = true,
        ValidAudience = _options.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = _signingKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = RecruitmentClaims.Name,
        RoleClaimType = RecruitmentClaims.Role
    };
}
