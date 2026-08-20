namespace IRA.Api.Configuration;

/// <summary>
/// Settings for the self-issued JWT bearer scheme, giving the API real token-based
/// authentication out of the box. In production, store <see cref="SigningKey"/> in
/// Azure Key Vault rather than appsettings.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "IntelligentRecruitmentAssistant";
    public string Audience { get; set; } = "IntelligentRecruitmentAssistant.Clients";

    /// <summary>HMAC-SHA256 signing key. Must be at least 32 characters (256 bits).</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access token lifetime in minutes.</summary>
    public int AccessTokenMinutes { get; set; } = 120;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SigningKey) && SigningKey.Length >= 32;
}
