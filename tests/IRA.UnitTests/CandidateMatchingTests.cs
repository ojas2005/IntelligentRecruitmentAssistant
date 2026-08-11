using IRA.Application.Abstractions.Agents;
using IRA.Domain.Entities;
using IRA.Domain.Rules;
using IRA.Domain.ValueObjects;
using Xunit;

namespace IRA.UnitTests;

/// <summary>Candidate Matching Testing — the Job Matching Agent's evaluation output.</summary>
public class CandidateMatchingTests
{
    [Fact]
    public async Task JobMatchingAgent_computes_score_gaps_and_attaches_citations()
    {
        var provider = TestFactory.CreateProvider();
        var agent = provider.GetRequiredService<IJobMatchingAgent>();

        var candidate = new Candidate("Bob Smith");
        candidate.AddSkill(new Skill("C#"));
        candidate.AddSkill(new Skill("Azure"));
        candidate.AddExperience(new Experience { Company = "Y", Role = "Dev", Years = 4 });

        var job = new JobDescription("Cloud Engineer", "Build cloud apps", minYearsExperience: 3);
        job.AddRequiredSkill(new Skill("C#"));
        job.AddRequiredSkill(new Skill("Azure"));
        job.AddRequiredSkill(new Skill("Kubernetes"));

        var citations = new[] { new Citation("src1", "Cloud Engineer JD", "Requires Kubernetes and C#", 0.9) };

        var evaluation = await agent.EvaluateAsync(candidate, job, citations);

        Assert.Equal(candidate.Id, evaluation.CandidateId);
        Assert.True(evaluation.FitScore.Value is > 0 and <= 100);
        Assert.Contains(evaluation.MatchedSkills, s => s == "C#");
        Assert.Contains(evaluation.SkillGaps, g => g.SkillName == "Kubernetes" && g.Required);
        Assert.Single(evaluation.Citations);
        Assert.False(string.IsNullOrWhiteSpace(evaluation.Summary));
    }

    [Fact]
    public async Task RankingAgent_orders_candidates_and_recommends()
    {
        var provider = TestFactory.CreateProvider();
        var ranking = provider.GetRequiredService<IRankingAgent>();

        var job = new JobDescription("Role", "desc");
        var evalHigh = new CandidateEvaluation(Guid.NewGuid(), "Top", job.Id, new FitScore(90));
        evalHigh.SetNarrative("s", "r");
        var evalLow = new CandidateEvaluation(Guid.NewGuid(), "Bottom", job.Id, new FitScore(35));
        evalLow.SetNarrative("s", "r");

        var result = await ranking.RankAsync(job, new[] { evalLow, evalHigh });

        Assert.Equal("Top", result.Candidates[0].CandidateName);
        Assert.Equal(1, result.Candidates[0].Rank);
        Assert.Equal(RecommendationPolicy.FromScore(new FitScore(90)), result.Candidates[0].Recommendation);
    }
}
