using IRA.Domain.Entities;
using IRA.Domain.Enums;
using IRA.Domain.ValueObjects;
using IRA.Infrastructure.Persistence.Cosmos;
using Xunit;

namespace IRA.UnitTests;

/// <summary>
/// Verifies the Cosmos persistence documents round-trip domain aggregates without loss —
/// including identity and timestamps restored via <c>EntityRestore</c> (reflection over the
/// protected Entity setters). These run offline (no live Cosmos required).
/// </summary>
public class CosmosMappingTests
{
    [Fact]
    public void Candidate_round_trips_with_identity_skills_and_experience()
    {
        var candidate = new Candidate("Grace Hopper", "grace@example.com", "+1 555", "Remote");
        candidate.AddSkill(new Skill("C#", SkillCategory.Technical, 10));
        candidate.AddSkill(new Skill("Azure"));
        candidate.AddExperience(new Experience { Company = "Navy", Role = "Engineer", Years = 7 });
        candidate.SetTotalYearsExperience(10);

        var restored = CandidateDocument.From(candidate).ToDomain();

        Assert.Equal(candidate.Id, restored.Id);
        Assert.Equal(candidate.CreatedAtUtc, restored.CreatedAtUtc);
        Assert.Equal("Grace Hopper", restored.FullName);
        Assert.Equal(candidate.Email, restored.Email);
        Assert.Equal(10, restored.TotalYearsExperience);
        Assert.Contains(restored.Skills, s => s.Name == "C#" && s.YearsOfExperience == 10);
        Assert.Contains(restored.Skills, s => s.Name == "Azure");
        Assert.Single(restored.Experiences);
    }

    [Fact]
    public void JobDescription_round_trips_required_and_preferred_skills()
    {
        var job = new JobDescription("Senior Backend Engineer", "Build APIs.", "Engineering", 5);
        job.AddRequiredSkill(new Skill("C#"));
        job.AddPreferredSkill(new Skill("Docker"));

        var restored = JobDescriptionDocument.From(job).ToDomain();

        Assert.Equal(job.Id, restored.Id);
        Assert.Equal("Senior Backend Engineer", restored.Title);
        Assert.Equal(5, restored.MinYearsExperience);
        Assert.Contains(restored.RequiredSkills, s => s.Name == "C#");
        Assert.Contains(restored.PreferredSkills, s => s.Name == "Docker");
    }

    [Fact]
    public void Evaluation_round_trips_score_gaps_citations_and_review()
    {
        var evaluation = new CandidateEvaluation(Guid.NewGuid(), "Grace", Guid.NewGuid(), new FitScore(87.5));
        evaluation.SetNarrative("Strong fit", "Meets required skills");
        evaluation.AddMatchedSkill("C#");
        evaluation.AddSkillGap(new SkillGap("Kubernetes", Required: true, Severity: 1.0));
        evaluation.AddCitation(new Citation("src1", "Backend JD", "requires C#", 0.91));
        evaluation.ApplyReview(true, "Looks good");

        var restored = EvaluationDocument.From(evaluation).ToDomain();

        Assert.Equal(evaluation.Id, restored.Id);
        Assert.Equal(87.5, restored.FitScore.Value);
        Assert.True(restored.ReviewerApproved);
        Assert.Equal("Looks good", restored.ReviewerNotes);
        Assert.Contains("C#", restored.MatchedSkills);
        Assert.Contains(restored.SkillGaps, g => g.SkillName == "Kubernetes" && g.Required);
        Assert.Contains(restored.Citations, c => c.SourceName == "Backend JD");
    }

    [Fact]
    public void Ranking_round_trips_and_preserves_descending_order()
    {
        var jobId = Guid.NewGuid();
        var ranking = new CandidateRanking(jobId, "Backend Engineer");
        ranking.SetCandidates(new[]
        {
            new RankedCandidate { CandidateId = Guid.NewGuid(), CandidateName = "Low", Score = new FitScore(40), Recommendation = RecommendationLevel.Consider, Justification = "ok" },
            new RankedCandidate { CandidateId = Guid.NewGuid(), CandidateName = "High", Score = new FitScore(95), Recommendation = RecommendationLevel.StrongShortlist, Justification = "great" },
        });
        ranking.MarkReviewed(true);

        var restored = RankingDocument.From(ranking).ToDomain();

        Assert.Equal(ranking.Id, restored.Id);
        Assert.Equal(jobId, restored.JobDescriptionId);
        Assert.True(restored.ReviewerApproved);
        Assert.Equal("High", restored.Candidates[0].CandidateName); // rank 1 == highest score
        Assert.Equal(1, restored.Candidates[0].Rank);
    }

    [Fact]
    public void Resume_round_trips_state_machine_and_candidate_link()
    {
        var resume = new Resume("grace.pdf", "resumes/grace.pdf");
        resume.MarkExtracted("Grace Hopper, C#");
        var candidateId = Guid.NewGuid();
        resume.MarkParsed(candidateId);
        resume.MarkEmbedded();

        var restored = ResumeDocument.From(resume).ToDomain();

        Assert.Equal(resume.Id, restored.Id);
        Assert.Equal(ResumeStatus.Embedded, restored.Status);
        Assert.Equal(candidateId, restored.CandidateId);
        Assert.Equal("Grace Hopper, C#", restored.RawText);
    }

    [Fact]
    public void AuditEntry_round_trips_with_timestamp()
    {
        var entry = new AuditEntry("recruiter", "EvaluationCompleted", "JobDescription", "job-1", "3 candidates");

        var restored = AuditDocument.From(entry).ToDomain();

        Assert.Equal(entry.Id, restored.Id);
        Assert.Equal(entry.CreatedAtUtc, restored.CreatedAtUtc);
        Assert.Equal("EvaluationCompleted", restored.Action);
        Assert.Equal("job-1", restored.EntityId);
    }
}
