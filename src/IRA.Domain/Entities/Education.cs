namespace IRA.Domain.Entities;

/// <summary>An academic qualification parsed from a resume.</summary>
public class Education
{
    public string Institution { get; init; } = string.Empty;
    public string Degree { get; init; } = string.Empty;
    public string? FieldOfStudy { get; init; }
    public int? GraduationYear { get; init; }
}
