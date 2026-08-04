using System.Text.RegularExpressions;
using IRA.Application.Abstractions.AI;
using IRA.Application.Abstractions.Agents;
using IRA.Application.DTOs;
using IRA.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace IRA.Infrastructure.Agents;

/// <summary>
/// Resume Parser Agent implementation. Uses Azure OpenAI (via Semantic Kernel) to extract
/// a structured profile; falls back to deterministic heuristics if the model is unavailable.
/// </summary>
public partial class ResumeParserAgent : IResumeParserAgent
{
    private const string SystemPrompt =
        "You are a precise resume parsing agent for a recruitment system. " +
        "Extract the candidate's structured profile from the resume text. " +
        "Respond ONLY with JSON matching this shape: " +
        "{\"fullName\":string,\"email\":string,\"phone\":string,\"location\":string," +
        "\"totalYearsExperience\":number,\"skills\":[{\"name\":string,\"category\":\"Technical|Soft|Domain|Certification|Tool|Language\"}]," +
        "\"experiences\":[{\"company\":string,\"role\":string,\"years\":number}]," +
        "\"education\":[{\"institution\":string,\"degree\":string,\"fieldOfStudy\":string}]," +
        "\"certifications\":[{\"name\":string,\"issuer\":string}]}.";

    private readonly ITextGenerationService _textGeneration;
    private readonly ILogger<ResumeParserAgent> _logger;

    public ResumeParserAgent(ITextGenerationService textGeneration, ILogger<ResumeParserAgent> logger)
    {
        _textGeneration = textGeneration;
        _logger = logger;
    }

    public async Task<ParsedResumeDto> ParseAsync(string resumeText, CancellationToken cancellationToken = default)
    {
        if (_textGeneration.IsLive)
        {
            try
            {
                var response = await _textGeneration.CompleteAsync(SystemPrompt, resumeText, cancellationToken);
                var parsed = AgentJson.TryDeserialize<ParsedResumeDto>(response);
                if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.FullName))
                {
                    return parsed;
                }

                _logger.LogWarning("Resume parser AI response could not be parsed; using fallback.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resume parser agent AI call failed; using deterministic fallback.");
            }
        }

        return FallbackParse(resumeText);
    }

    /// <summary>Deterministic heuristic parser used when Azure OpenAI is unavailable.</summary>
    internal static ParsedResumeDto FallbackParse(string text)
    {
        text ??= string.Empty;
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        var email = EmailRegex().Match(text).Value;
        var phone = PhoneRegex().Match(text).Value;

        var name = lines.FirstOrDefault(l =>
                       !l.Contains('@') &&
                       !DigitRegex().IsMatch(l) &&
                       l.Length is > 2 and < 60)
                   ?? "Unknown Candidate";

        var skills = SkillKeywords.Known
            .Where(k => text.Contains(k, StringComparison.OrdinalIgnoreCase))
            .Select(k => new ParsedSkillDto(k, SkillCategory.Technical))
            .ToList();

        var years = 0.0;
        var yearsMatch = ExperienceRegex().Match(text);
        if (yearsMatch.Success && double.TryParse(yearsMatch.Groups[1].Value, out var y))
        {
            years = y;
        }

        return new ParsedResumeDto
        {
            FullName = name,
            Email = string.IsNullOrEmpty(email) ? null : email,
            Phone = string.IsNullOrEmpty(phone) ? null : phone,
            Location = null,
            TotalYearsExperience = years,
            Skills = skills
        };
    }

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\+?\d[\d\s().-]{7,}\d")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"\d")]
    private static partial Regex DigitRegex();

    [GeneratedRegex(@"(\d+)\+?\s*years?", RegexOptions.IgnoreCase)]
    private static partial Regex ExperienceRegex();
}
