using IRA.Application.Abstractions.Agents;
using IRA.Domain.Entities;
using IRA.Domain.Rules;
using Microsoft.Extensions.Logging;

namespace IRA.Infrastructure.Agents;

/// <summary>
/// Reviewer Agent implementation. Validates AI-generated evaluations and rankings for
/// consistency and fairness before they are surfaced. Uses deterministic guardrail rules
/// (grounding, score bounds, ordering) so validation is reliable even without the LLM.
/// </summary>
public class ReviewerAgent : IReviewerAgent
{
    private readonly ILogger<ReviewerAgent> _logger;

    public ReviewerAgent(ILogger<ReviewerAgent> logger) => _logger = logger;

    public Task<ReviewResult> ReviewEvaluationAsync(
        CandidateEvaluation evaluation,
        JobDescription job,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();

        // Guardrail 1: score must be within valid bounds (FitScore already enforces, double-check).
        if (evaluation.FitScore.Value is < 0 or > 100)
        {
            issues.Add("Fit score out of range.");
        }

        // Guardrail 2: recommendations must remain grounded — a high score with zero matched
        // skills is inconsistent and rejected for fairness.
        var recommendation = RecommendationPolicy.FromScore(evaluation.FitScore);
        if (RecommendationPolicy.IsShortlisted(recommendation) && evaluation.MatchedSkills.Count == 0)
        {
            issues.Add("Shortlisted with no matched skills — inconsistent, flagged for recruiter review.");
        }

        // Guardrail 3: narrative must be present (traceability).
        if (string.IsNullOrWhiteSpace(evaluation.Summary))
        {
            issues.Add("Missing evaluation summary.");
        }

        var approved = issues.Count == 0;
        var notes = approved ? "Validated: consistent and grounded." : string.Join(" ", issues);
        if (!approved)
        {
            _logger.LogWarning("Reviewer flagged evaluation for {Candidate}: {Notes}", evaluation.CandidateName, notes);
        }

        return Task.FromResult(new ReviewResult(approved, notes));
    }

    public Task<ReviewResult> ReviewRankingAsync(CandidateRanking ranking, CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();

        // Ranking must be strictly ordered by descending score.
        for (var i = 1; i < ranking.Candidates.Count; i++)
        {
            if (ranking.Candidates[i].Score.Value > ranking.Candidates[i - 1].Score.Value)
            {
                issues.Add("Ranking not ordered by descending score.");
                break;
            }
        }

        // Ranks must be contiguous and 1-based.
        var expectedRank = 1;
        foreach (var c in ranking.Candidates)
        {
            if (c.Rank != expectedRank++)
            {
                issues.Add("Rank numbering is not contiguous.");
                break;
            }
        }

        var approved = issues.Count == 0;
        var notes = approved ? "Ranking validated." : string.Join(" ", issues);
        return Task.FromResult(new ReviewResult(approved, notes));
    }
}
