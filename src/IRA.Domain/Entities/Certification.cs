namespace IRA.Domain.Entities;

/// <summary>A professional certification parsed from a resume.</summary>
public class Certification
{
    public string Name { get; init; } = string.Empty;
    public string? Issuer { get; init; }
    public int? Year { get; init; }
}
