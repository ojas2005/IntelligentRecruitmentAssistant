using IRA.Domain.Entities;
using IRA.Domain.Enums;
using IRA.Domain.ValueObjects;

namespace IRA.Infrastructure.Persistence.Cosmos;

/*
 * Persistence documents: flat, serialisation-friendly projections of the domain aggregates,
 * plus explicit two-way mapping. Keeping these separate from the domain entities lets the
 * aggregates stay encapsulated (private setters, invariant-enforcing constructors) while Cosmos
 * stores plain data. Each document's Id is the Cosmos item id; EntityRestore reinstates the
 * original aggregate Id / CreatedAtUtc on the way back.
 */

// ----- Shared nested value documents -----

public sealed class SkillDoc
{
    public string Name { get; set; } = string.Empty;
    public SkillCategory Category { get; set; }
    public int? YearsOfExperience { get; set; }
}

public sealed class ExperienceDoc
{
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public double Years { get; set; }
    public string? Description { get; set; }
}

public sealed class EducationDoc
{
    public string Institution { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string? FieldOfStudy { get; set; }
    public int? GraduationYear { get; set; }
}

public sealed class CertificationDoc
{
    public string Name { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public int? Year { get; set; }
}

public sealed class SkillGapDoc
{
    public string SkillName { get; set; } = string.Empty;
    public bool Required { get; set; }
    public double Severity { get; set; }
}

public sealed class CitationDoc
{
    public string SourceId { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public double Score { get; set; }
}

public sealed class QuestionDoc
{
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public string? EvaluationCriteria { get; set; }
    public string? TargetSkill { get; set; }
}

public sealed class RankedCandidateDoc
{
    public Guid CandidateId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public double Score { get; set; }
    public RecommendationLevel Recommendation { get; set; }
    public string Justification { get; set; } = string.Empty;
}

// ----- Aggregate documents -----

public sealed class CandidateDocument
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public double TotalYearsExperience { get; set; }
    public List<SkillDoc> Skills { get; set; } = new();
    public List<ExperienceDoc> Experiences { get; set; } = new();
    public List<EducationDoc> Education { get; set; } = new();
    public List<CertificationDoc> Certifications { get; set; } = new();
    public DateTimeOffset CreatedAtUtc { get; set; }

    public static CandidateDocument From(Candidate c) => new()
    {
        Id = c.Id.ToString(),
        FullName = c.FullName,
        Email = c.Email,
        Phone = c.Phone,
        Location = c.Location,
        TotalYearsExperience = c.TotalYearsExperience,
        Skills = c.Skills.Select(s => new SkillDoc { Name = s.Name, Category = s.Category, YearsOfExperience = s.YearsOfExperience }).ToList(),
        Experiences = c.Experiences.Select(e => new ExperienceDoc { Company = e.Company, Role = e.Role, Years = e.Years, Description = e.Description }).ToList(),
        Education = c.Education.Select(e => new EducationDoc { Institution = e.Institution, Degree = e.Degree, FieldOfStudy = e.FieldOfStudy, GraduationYear = e.GraduationYear }).ToList(),
        Certifications = c.Certifications.Select(x => new CertificationDoc { Name = x.Name, Issuer = x.Issuer, Year = x.Year }).ToList(),
        CreatedAtUtc = c.CreatedAtUtc
    };

    public Candidate ToDomain()
    {
        var candidate = new Candidate(FullName, Email, Phone, Location);
        foreach (var s in Skills)
        {
            candidate.AddSkill(new Skill(s.Name, s.Category, s.YearsOfExperience));
        }
        foreach (var e in Experiences)
        {
            candidate.AddExperience(new Experience { Company = e.Company, Role = e.Role, Years = e.Years, Description = e.Description });
        }
        foreach (var e in Education)
        {
            candidate.AddEducation(new Education { Institution = e.Institution, Degree = e.Degree, FieldOfStudy = e.FieldOfStudy, GraduationYear = e.GraduationYear });
        }
        foreach (var x in Certifications)
        {
            candidate.AddCertification(new Certification { Name = x.Name, Issuer = x.Issuer, Year = x.Year });
        }
        candidate.SetTotalYearsExperience(TotalYearsExperience);
        return candidate.With(Guid.Parse(Id), CreatedAtUtc);
    }
}

public sealed class ResumeDocument
{
    public string Id { get; set; } = string.Empty;
    public Guid? CandidateId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
    public string? RawText { get; set; }
    public ResumeStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public static ResumeDocument From(Resume r) => new()
    {
        Id = r.Id.ToString(),
        CandidateId = r.CandidateId,
        FileName = r.FileName,
        BlobPath = r.BlobPath,
        RawText = r.RawText,
        Status = r.Status,
        FailureReason = r.FailureReason,
        CreatedAtUtc = r.CreatedAtUtc
    };

    public Resume ToDomain()
    {
        var resume = new Resume(FileName, BlobPath);
        if (!string.IsNullOrEmpty(RawText))
        {
            resume.MarkExtracted(RawText);
        }
        if (CandidateId is { } cid)
        {
            resume.MarkParsed(cid);
        }
        switch (Status)
        {
            case ResumeStatus.Embedded: resume.MarkEmbedded(); break;
            case ResumeStatus.Evaluated: resume.MarkEvaluated(); break;
            case ResumeStatus.Failed: resume.MarkFailed(FailureReason ?? string.Empty); break;
        }
        return resume.With(Guid.Parse(Id), CreatedAtUtc);
    }
}

public sealed class JobDescriptionDocument
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string RawText { get; set; } = string.Empty;
    public double MinYearsExperience { get; set; }
    public string BlobPath { get; set; } = string.Empty;
    public List<SkillDoc> RequiredSkills { get; set; } = new();
    public List<SkillDoc> PreferredSkills { get; set; } = new();
    public DateTimeOffset CreatedAtUtc { get; set; }

    public static JobDescriptionDocument From(JobDescription j) => new()
    {
        Id = j.Id.ToString(),
        Title = j.Title,
        Department = j.Department,
        RawText = j.RawText,
        MinYearsExperience = j.MinYearsExperience,
        BlobPath = j.BlobPath,
        RequiredSkills = j.RequiredSkills.Select(s => new SkillDoc { Name = s.Name, Category = s.Category, YearsOfExperience = s.YearsOfExperience }).ToList(),
        PreferredSkills = j.PreferredSkills.Select(s => new SkillDoc { Name = s.Name, Category = s.Category, YearsOfExperience = s.YearsOfExperience }).ToList(),
        CreatedAtUtc = j.CreatedAtUtc
    };

    public JobDescription ToDomain()
    {
        var job = new JobDescription(Title, RawText, Department, MinYearsExperience);
        foreach (var s in RequiredSkills)
        {
            job.AddRequiredSkill(new Skill(s.Name, s.Category, s.YearsOfExperience));
        }
        foreach (var s in PreferredSkills)
        {
            job.AddPreferredSkill(new Skill(s.Name, s.Category, s.YearsOfExperience));
        }
        job.SetBlobPath(BlobPath);
        return job.With(Guid.Parse(Id), CreatedAtUtc);
    }
}

public sealed class EvaluationDocument
{
    public string Id { get; set; } = string.Empty;
    public Guid CandidateId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public Guid JobDescriptionId { get; set; }
    public double FitScore { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public bool ReviewerApproved { get; set; }
    public string? ReviewerNotes { get; set; }
    public List<SkillGapDoc> SkillGaps { get; set; } = new();
    public List<CitationDoc> Citations { get; set; } = new();
    public List<string> MatchedSkills { get; set; } = new();
    public DateTimeOffset CreatedAtUtc { get; set; }

    public static EvaluationDocument From(CandidateEvaluation e) => new()
    {
        Id = e.Id.ToString(),
        CandidateId = e.CandidateId,
        CandidateName = e.CandidateName,
        JobDescriptionId = e.JobDescriptionId,
        FitScore = e.FitScore.Value,
        Summary = e.Summary,
        Reasoning = e.Reasoning,
        ReviewerApproved = e.ReviewerApproved,
        ReviewerNotes = e.ReviewerNotes,
        SkillGaps = e.SkillGaps.Select(g => new SkillGapDoc { SkillName = g.SkillName, Required = g.Required, Severity = g.Severity }).ToList(),
        Citations = e.Citations.Select(c => new CitationDoc { SourceId = c.SourceId, SourceName = c.SourceName, Snippet = c.Snippet, Score = c.Score }).ToList(),
        MatchedSkills = e.MatchedSkills.ToList(),
        CreatedAtUtc = e.CreatedAtUtc
    };

    public CandidateEvaluation ToDomain()
    {
        var evaluation = new CandidateEvaluation(CandidateId, CandidateName, JobDescriptionId, new FitScore(FitScore));
        evaluation.SetNarrative(Summary, Reasoning);
        foreach (var g in SkillGaps)
        {
            evaluation.AddSkillGap(new SkillGap(g.SkillName, g.Required, g.Severity));
        }
        foreach (var m in MatchedSkills)
        {
            evaluation.AddMatchedSkill(m);
        }
        evaluation.AddCitations(Citations.Select(c => new Citation(c.SourceId, c.SourceName, c.Snippet, c.Score)));
        evaluation.ApplyReview(ReviewerApproved, ReviewerNotes);
        return evaluation.With(Guid.Parse(Id), CreatedAtUtc);
    }
}

public sealed class InterviewKitDocument
{
    public string Id { get; set; } = string.Empty;
    public Guid CandidateId { get; set; }
    public Guid JobDescriptionId { get; set; }
    public List<QuestionDoc> Questions { get; set; } = new();
    public DateTimeOffset CreatedAtUtc { get; set; }

    public static InterviewKitDocument From(InterviewKit k) => new()
    {
        Id = k.Id.ToString(),
        CandidateId = k.CandidateId,
        JobDescriptionId = k.JobDescriptionId,
        Questions = k.Questions.Select(q => new QuestionDoc
        {
            Text = q.Text, Type = q.Type, Difficulty = q.Difficulty,
            EvaluationCriteria = q.EvaluationCriteria, TargetSkill = q.TargetSkill
        }).ToList(),
        CreatedAtUtc = k.CreatedAtUtc
    };

    public InterviewKit ToDomain()
    {
        var kit = new InterviewKit(CandidateId, JobDescriptionId);
        kit.AddRange(Questions.Select(q => new InterviewQuestion
        {
            Text = q.Text, Type = q.Type, Difficulty = q.Difficulty,
            EvaluationCriteria = q.EvaluationCriteria, TargetSkill = q.TargetSkill
        }));
        return kit.With(Guid.Parse(Id), CreatedAtUtc);
    }
}

public sealed class RankingDocument
{
    /// <summary>Cosmos id and partition key == the job description id (one ranking per job).</summary>
    public string Id { get; set; } = string.Empty;
    public Guid RankingId { get; set; }
    public Guid JobDescriptionId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public bool ReviewerApproved { get; set; }
    public List<RankedCandidateDoc> Candidates { get; set; } = new();
    public DateTimeOffset CreatedAtUtc { get; set; }

    public static RankingDocument From(CandidateRanking r) => new()
    {
        Id = r.JobDescriptionId.ToString(),
        RankingId = r.Id,
        JobDescriptionId = r.JobDescriptionId,
        JobTitle = r.JobTitle,
        ReviewerApproved = r.ReviewerApproved,
        Candidates = r.Candidates.Select(c => new RankedCandidateDoc
        {
            CandidateId = c.CandidateId, CandidateName = c.CandidateName, Rank = c.Rank,
            Score = c.Score.Value, Recommendation = c.Recommendation, Justification = c.Justification
        }).ToList(),
        CreatedAtUtc = r.CreatedAtUtc
    };

    public CandidateRanking ToDomain()
    {
        var ranking = new CandidateRanking(JobDescriptionId, JobTitle);
        ranking.SetCandidates(Candidates.Select(c => new RankedCandidate
        {
            CandidateId = c.CandidateId,
            CandidateName = c.CandidateName,
            Score = new FitScore(c.Score),
            Recommendation = c.Recommendation,
            Justification = c.Justification
        }));
        ranking.MarkReviewed(ReviewerApproved);
        return ranking.With(RankingId, CreatedAtUtc);
    }
}

public sealed class AuditDocument
{
    public string Id { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }

    public static AuditDocument From(AuditEntry e) => new()
    {
        Id = e.Id.ToString(),
        Actor = e.Actor,
        Action = e.Action,
        EntityType = e.EntityType,
        EntityId = e.EntityId,
        Details = e.Details,
        TimestampUtc = e.CreatedAtUtc
    };

    public AuditEntry ToDomain() =>
        new AuditEntry(Actor, Action, EntityType, EntityId, Details).With(Guid.Parse(Id), TimestampUtc);
}
