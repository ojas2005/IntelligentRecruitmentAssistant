using IRA.Domain.Enums;

namespace IRA.Application.DTOs;

public record SkillGapDto(string SkillName, bool Required, double Severity);

public record CitationDto(string SourceId, string SourceName, string Snippet, double Score);

/// <summary>Full evaluation of a candidate against a role, including grounding citations.</summary>
public record CandidateEvaluationDto
{
    public Guid CandidateId { get; init; }
    public string CandidateName { get; init; } = string.Empty;
    public Guid JobDescriptionId { get; init; }
    public double FitScore { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string Reasoning { get; init; } = string.Empty;
    public bool ReviewerApproved { get; init; }
    public string? ReviewerNotes { get; init; }
    public IReadOnlyList<string> MatchedSkills { get; init; } = Array.Empty<string>();
    public IReadOnlyList<SkillGapDto> SkillGaps { get; init; } = Array.Empty<SkillGapDto>();
    public IReadOnlyList<CitationDto> Citations { get; init; } = Array.Empty<CitationDto>();
}

public record InterviewQuestionDto(
    string Text,
    QuestionType Type,
    DifficultyLevel Difficulty,
    string? EvaluationCriteria,
    string? TargetSkill);

public record InterviewKitDto(
    Guid CandidateId,
    Guid JobDescriptionId,
    IReadOnlyList<InterviewQuestionDto> Questions);

public record RankedCandidateDto(
    Guid CandidateId,
    string CandidateName,
    int Rank,
    double Score,
    RecommendationLevel Recommendation,
    string Justification);

public record CandidateRankingDto(
    Guid JobDescriptionId,
    string JobTitle,
    bool ReviewerApproved,
    IReadOnlyList<RankedCandidateDto> Candidates);

/// <summary>Request to run the end-to-end evaluation workflow for a job.</summary>
public record EvaluateCandidatesRequestDto
{
    public Guid JobDescriptionId { get; init; }

    /// <summary>Optional subset of candidates; when empty, all candidates are evaluated.</summary>
    public List<Guid> CandidateIds { get; init; } = new();

    /// <summary>How many top candidates should receive generated interview kits.</summary>
    public int InterviewShortlistSize { get; init; } = 5;
}

/// <summary>Aggregated output of the full Extract -> Analyze -> Match -> Generate -> Rank flow.</summary>
public record RecruitmentEvaluationResultDto
{
    public CandidateRankingDto Ranking { get; init; } = null!;
    public IReadOnlyList<CandidateEvaluationDto> Evaluations { get; init; } = Array.Empty<CandidateEvaluationDto>();
    public IReadOnlyList<InterviewKitDto> InterviewKits { get; init; } = Array.Empty<InterviewKitDto>();
    public bool UsedAiFallback { get; init; }
}

public record AuditEntryDto(
    Guid Id,
    DateTimeOffset TimestampUtc,
    string Actor,
    string Action,
    string EntityType,
    string? EntityId,
    string? Details);
