using IRA.Domain.Enums;

namespace IRA.Application.DTOs;

/// <summary>Structured output of the Resume Parser Agent (Extract step).</summary>
public record ParsedResumeDto
{
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Location { get; init; }
    public double TotalYearsExperience { get; init; }
    public List<ParsedSkillDto> Skills { get; init; } = new();
    public List<ParsedExperienceDto> Experiences { get; init; } = new();
    public List<ParsedEducationDto> Education { get; init; } = new();
    public List<ParsedCertificationDto> Certifications { get; init; } = new();
}

public record ParsedSkillDto(string Name, SkillCategory Category = SkillCategory.Technical, int? YearsOfExperience = null);

public record ParsedExperienceDto(string Company, string Role, double Years, string? Description = null);

public record ParsedEducationDto(string Institution, string Degree, string? FieldOfStudy = null, int? GraduationYear = null);

public record ParsedCertificationDto(string Name, string? Issuer = null, int? Year = null);

/// <summary>Returned to the caller after a resume upload.</summary>
public record ResumeUploadResultDto(Guid ResumeId, Guid CandidateId, string CandidateName, ResumeStatus Status);

/// <summary>A candidate summary suitable for list/dashboard display.</summary>
public record CandidateDto(
    Guid Id,
    string FullName,
    string? Email,
    string? Location,
    double TotalYearsExperience,
    IReadOnlyList<string> Skills);
