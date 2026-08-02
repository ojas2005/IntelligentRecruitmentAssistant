using IRA.Application.DTOs;

namespace IRA.Application.Abstractions.Agents;

/// <summary>
/// Resume Parser Agent — extracts candidate attributes, skills, certifications,
/// experience and education from raw resume text. First stage of the flow (Extract).
/// </summary>
public interface IResumeParserAgent
{
    Task<ParsedResumeDto> ParseAsync(string resumeText, CancellationToken cancellationToken = default);
}
