using IRA.Application.DTOs;
using IRA.Domain.Entities;
using IRA.Domain.Rules;

namespace IRA.Application.Mapping;

/// <summary>Pure mapping helpers between domain entities and transport DTOs.</summary>
public static class DomainMappings
{
    public static CandidateDto ToDto(this Candidate c) => new(
        c.Id,
        c.FullName,
        c.Email,
        c.Location,
        c.TotalYearsExperience,
        c.Skills.Select(s => s.Name).ToList());

    public static JobDescriptionDto ToDto(this JobDescription j) => new(
        j.Id,
        j.Title,
        j.Department,
        j.MinYearsExperience,
        j.RequiredSkills.Select(s => s.Name).ToList(),
        j.PreferredSkills.Select(s => s.Name).ToList());

    public static CandidateEvaluationDto ToDto(this CandidateEvaluation e) => new()
    {
        CandidateId = e.CandidateId,
        CandidateName = e.CandidateName,
        JobDescriptionId = e.JobDescriptionId,
        FitScore = e.FitScore.Value,
        Summary = e.Summary,
        Reasoning = e.Reasoning,
        ReviewerApproved = e.ReviewerApproved,
        ReviewerNotes = e.ReviewerNotes,
        MatchedSkills = e.MatchedSkills.ToList(),
        SkillGaps = e.SkillGaps.Select(g => new SkillGapDto(g.SkillName, g.Required, g.Severity)).ToList(),
        Citations = e.Citations.Select(c => new CitationDto(c.SourceId, c.SourceName, c.Snippet, c.Score)).ToList()
    };

    public static InterviewKitDto ToDto(this InterviewKit kit) => new(
        kit.CandidateId,
        kit.JobDescriptionId,
        kit.Questions.Select(q => new InterviewQuestionDto(
            q.Text, q.Type, q.Difficulty, q.EvaluationCriteria, q.TargetSkill)).ToList());

    public static CandidateRankingDto ToDto(this CandidateRanking r) => new(
        r.JobDescriptionId,
        r.JobTitle,
        r.ReviewerApproved,
        r.Candidates.Select(c => new RankedCandidateDto(
            c.CandidateId, c.CandidateName, c.Rank, c.Score.Value, c.Recommendation, c.Justification)).ToList());

    public static AuditEntryDto ToDto(this AuditEntry a) => new(
        a.Id, a.CreatedAtUtc, a.Actor, a.Action, a.EntityType, a.EntityId, a.Details);
}
