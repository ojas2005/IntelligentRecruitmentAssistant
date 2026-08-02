namespace IRA.Application.DTOs;

/// <summary>Request to register a job description.</summary>
public record CreateJobDescriptionDto
{
    public string Title { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string RawText { get; init; } = string.Empty;
    public double MinYearsExperience { get; init; }
    public List<string> RequiredSkills { get; init; } = new();
    public List<string> PreferredSkills { get; init; } = new();
}

public record JobDescriptionDto(
    Guid Id,
    string Title,
    string? Department,
    double MinYearsExperience,
    IReadOnlyList<string> RequiredSkills,
    IReadOnlyList<string> PreferredSkills);
