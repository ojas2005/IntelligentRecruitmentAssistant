using IRA.Application.Abstractions.Agents;
using IRA.Domain.Entities;
using IRA.Domain.ValueObjects;
using Xunit;

namespace IRA.UnitTests;

/// <summary>Reviewer Agent validation — fairness/consistency guardrails.</summary>
public class ReviewerAgentTests
{
    [Fact]
    public async Task Approves_a_grounded_consistent_evaluation()
    {
        var provider = TestFactory.CreateProvider();
        var reviewer = provider.GetRequiredService<IReviewerAgent>();

        var job = new JobDescription("Role", "desc");
        var eval = new CandidateEvaluation(Guid.NewGuid(), "Sound", job.Id, new FitScore(80));
        eval.AddMatchedSkill("C#");
        eval.SetNarrative("Good fit", "Matches core skills.");

        var review = await reviewer.ReviewEvaluationAsync(eval, job);
        Assert.True(review.Approved);
    }

    [Fact]
    public async Task Flags_a_shortlisted_evaluation_with_no_matched_skills()
    {
        var provider = TestFactory.CreateProvider();
        var reviewer = provider.GetRequiredService<IReviewerAgent>();

        var job = new JobDescription("Role", "desc");
        // High score but no matched skills and no summary -> inconsistent.
        var eval = new CandidateEvaluation(Guid.NewGuid(), "Suspicious", job.Id, new FitScore(88));

        var review = await reviewer.ReviewEvaluationAsync(eval, job);
        Assert.False(review.Approved);
    }
}
