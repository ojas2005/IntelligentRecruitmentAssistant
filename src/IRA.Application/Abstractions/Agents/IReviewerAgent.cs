using IRA.Domain.Entities;

namespace IRA.Application.Abstractions.Agents;

/// <summary>Outcome of a reviewer validation pass.</summary>
public record ReviewResult(bool Approved, string Notes);

/// <summary>
/// Reviewer Agent — validates AI-generated evaluations and rankings for consistency
/// and fairness before results are surfaced to recruiters.
/// </summary>
public interface IReviewerAgent
{
    Task<ReviewResult> ReviewEvaluationAsync(
        CandidateEvaluation evaluation,
        JobDescription job,
        CancellationToken cancellationToken = default);

    Task<ReviewResult> ReviewRankingAsync(
        CandidateRanking ranking,
        CancellationToken cancellationToken = default);
}
