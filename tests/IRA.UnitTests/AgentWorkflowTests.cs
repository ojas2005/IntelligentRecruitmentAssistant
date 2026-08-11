using System.Text;
using IRA.Application.Abstractions.Orchestration;
using IRA.Application.DTOs;
using IRA.Application.Services;
using Xunit;

namespace IRA.UnitTests;

/// <summary>
/// AI Agent Workflow Testing — the end-to-end orchestrated flow:
/// Extract → Analyze → Match → Generate Questions → Rank, with Reviewer validation.
/// </summary>
public class AgentWorkflowTests
{
    private static async Task<(IServiceProvider Provider, Guid JobId)> SeedAsync()
    {
        var provider = TestFactory.CreateProvider();
        var resumeService = provider.GetRequiredService<ResumeProcessingService>();
        var jobService = provider.GetRequiredService<JobDescriptionService>();

        // Two candidates with differing fit.
        var strong = """
            Grace Hopper
            grace@example.com
            10 years experience.
            Skills: C#, ASP.NET Core, Azure, Docker, SQL
            """;
        var weak = """
            Pat Novice
            pat@example.com
            1 years experience.
            Skills: HTML, CSS
            """;

        await resumeService.ProcessAsync(new MemoryStream(Encoding.UTF8.GetBytes(strong)), "grace.txt", "tester");
        await resumeService.ProcessAsync(new MemoryStream(Encoding.UTF8.GetBytes(weak)), "pat.txt", "tester");

        var job = await jobService.CreateAsync(new CreateJobDescriptionDto
        {
            Title = "Senior Backend Engineer",
            RawText = "We need a senior engineer with strong C#, ASP.NET Core and Azure skills.",
            MinYearsExperience = 5,
            RequiredSkills = new List<string> { "C#", "ASP.NET Core", "Azure" },
            PreferredSkills = new List<string> { "Docker" }
        }, "tester");

        return (provider, job.Id);
    }

    [Fact]
    public async Task Full_evaluation_flow_produces_ranking_evaluations_and_interviews()
    {
        var (provider, jobId) = await SeedAsync();
        var orchestrator = provider.GetRequiredService<IRecruitmentOrchestrator>();

        var result = await orchestrator.RunEvaluationAsync(
            new EvaluateCandidatesRequestDto { JobDescriptionId = jobId, InterviewShortlistSize = 1 },
            "tester");

        // Ranking produced and ordered.
        Assert.Equal(2, result.Ranking.Candidates.Count);
        Assert.Equal("Grace Hopper", result.Ranking.Candidates[0].CandidateName);
        Assert.True(result.Ranking.Candidates[0].Score >= result.Ranking.Candidates[1].Score);
        Assert.True(result.Ranking.ReviewerApproved);

        // Evaluations grounded with citations.
        Assert.Equal(2, result.Evaluations.Count);
        Assert.All(result.Evaluations, e => Assert.False(string.IsNullOrWhiteSpace(e.Summary)));

        // Interview kit generated for the shortlist.
        Assert.Single(result.InterviewKits);
        Assert.NotEmpty(result.InterviewKits[0].Questions);

        // Offline fallback should be flagged (no Azure keys in tests).
        Assert.True(result.UsedAiFallback);
    }

    [Fact]
    public async Task Evaluation_writes_an_audit_trail()
    {
        var (provider, jobId) = await SeedAsync();
        var orchestrator = provider.GetRequiredService<IRecruitmentOrchestrator>();
        await orchestrator.RunEvaluationAsync(new EvaluateCandidatesRequestDto { JobDescriptionId = jobId }, "auditor");

        var audit = provider.GetRequiredService<IRA.Application.Abstractions.Audit.IAuditLogger>();
        var entries = await audit.GetRecentAsync(100);

        Assert.Contains(entries, e => e.Action == "EvaluationStarted");
        Assert.Contains(entries, e => e.Action == "EvaluationCompleted");
    }
}
