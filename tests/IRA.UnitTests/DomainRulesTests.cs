using IRA.Domain.Common;
using IRA.Domain.Entities;
using IRA.Domain.Enums;
using IRA.Domain.Rules;
using IRA.Domain.ValueObjects;
using Xunit;

namespace IRA.UnitTests;

/// <summary>Unit Testing — core domain invariants and business rules.</summary>
public class DomainRulesTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(double.NaN)]
    public void FitScore_rejects_out_of_range_values(double value)
    {
        Assert.Throws<DomainException>(() => new FitScore(value));
    }

    [Fact]
    public void FitScore_from_fraction_clamps_and_scales()
    {
        Assert.Equal(100, new FitScore(100).Value);
        Assert.Equal(50, FitScore.FromFraction(0.5).Value);
        Assert.Equal(100, FitScore.FromFraction(1.4).Value); // clamped
    }

    [Theory]
    [InlineData(90, RecommendationLevel.StrongShortlist)]
    [InlineData(75, RecommendationLevel.Shortlist)]
    [InlineData(60, RecommendationLevel.Consider)]
    [InlineData(40, RecommendationLevel.Reject)]
    [InlineData(10, RecommendationLevel.StrongReject)]
    public void RecommendationPolicy_maps_scores(double score, RecommendationLevel expected)
    {
        Assert.Equal(expected, RecommendationPolicy.FromScore(new FitScore(score)));
    }

    [Fact]
    public void SkillMatching_full_overlap_scores_high()
    {
        var candidate = new Candidate("Jane Dev");
        candidate.AddSkill(new Skill("C#"));
        candidate.AddSkill(new Skill("ASP.NET Core"));
        candidate.AddSkill(new Skill("Azure"));
        candidate.AddExperience(new Experience { Company = "X", Role = "Dev", Years = 6 });

        var job = new JobDescription("Backend Developer", "desc", minYearsExperience: 5);
        job.AddRequiredSkill(new Skill("C#"));
        job.AddRequiredSkill(new Skill("ASP.NET Core"));

        var score = SkillMatchingRules.ComputeFitScore(candidate, job);
        Assert.True(score.Value >= 85, $"Expected strong fit but got {score.Value}");
        Assert.Empty(SkillMatchingRules.IdentifySkillGaps(candidate, job));
    }

    [Fact]
    public void SkillMatching_identifies_missing_required_skills()
    {
        var candidate = new Candidate("John Doe");
        candidate.AddSkill(new Skill("Java"));

        var job = new JobDescription("Dotnet Dev", "desc");
        job.AddRequiredSkill(new Skill("C#"));
        job.AddRequiredSkill(new Skill("Azure"));

        var gaps = SkillMatchingRules.IdentifySkillGaps(candidate, job);
        Assert.Equal(2, gaps.Count);
        Assert.All(gaps, g => Assert.True(g.Required));
    }

    [Fact]
    public void Ranking_orders_by_descending_score_and_assigns_ranks()
    {
        var ranking = new CandidateRanking(Guid.NewGuid(), "Role");
        ranking.SetCandidates(new[]
        {
            new RankedCandidate { CandidateName = "Low", Score = new FitScore(40) },
            new RankedCandidate { CandidateName = "High", Score = new FitScore(92) },
            new RankedCandidate { CandidateName = "Mid", Score = new FitScore(70) }
        });

        Assert.Equal("High", ranking.Candidates[0].CandidateName);
        Assert.Equal(1, ranking.Candidates[0].Rank);
        Assert.Equal("Mid", ranking.Candidates[1].CandidateName);
        Assert.Equal("Low", ranking.Candidates[2].CandidateName);
        Assert.Equal(3, ranking.Candidates[2].Rank);
    }

    [Fact]
    public void Candidate_deduplicates_skills_case_insensitively()
    {
        var candidate = new Candidate("Dup");
        candidate.AddSkill(new Skill("C#"));
        candidate.AddSkill(new Skill("c#"));
        Assert.Single(candidate.Skills);
    }
}
