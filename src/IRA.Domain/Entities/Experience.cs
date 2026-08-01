namespace IRA.Domain.Entities;

/// <summary>A single professional experience item parsed from a resume.</summary>
public class Experience
{
    public string Company { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public double Years { get; init; }
    public string? Description { get; init; }
}
