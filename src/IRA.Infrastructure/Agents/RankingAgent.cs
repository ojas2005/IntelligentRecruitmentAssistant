using IRA.Application.Abstractions.Agents;
using IRA.Domain.Entities;
using IRA.Domain.Rules;
using Microsoft.Extensions.Logging;

namespace IRA.Infrastructure.Agents;

/// <summary>
/// Ranking Agent implementation. Aggregates validated evaluations, assigns recommendation
/// levels via the domain <see cref="RecommendationPolicy"/> and produces the prioritised
/// shortlist ordered by fit score.
/// </summary>
public class RankingAgent : IRankingAgent
{
    private readonly ILogger<RankingAgent> _logger;

    public RankingAgent(ILogger<RankingAgent> logger) => _logger = logger;

    public Task<CandidateRanking> RankAsync(
        JobDescription job,
        IReadOnlyList<CandidateEvaluation> evaluations,
        CancellationToken cancellationToken = default)
    {
        var ranking = new CandidateRanking(job.Id, job.Title);

        var ranked = evaluations.Select(e =>
        {
            var recommendation = RecommendationPolicy.FromScore(e.FitScore);
            var justification = BuildJustification(e, recommendation);
            return new RankedCandidate
            {
                CandidateId = e.CandidateId,
                CandidateName = e.CandidateName,
                Score = e.FitScore,
                Recommendation = recommendation,
                Justification = justification
            };
        }).ToList();

        // The domain entity re-sorts by descending score and assigns 1-based ranks.
        ranking.SetCandidates(ranked);
        _logger.LogInformation("Ranking produced for '{Job}' with {Count} candidates.", job.Title, ranked.Count);

        return Task.FromResult(ranking);
    }

    private static string BuildJustification(CandidateEvaluation evaluation, Domain.Enums.RecommendationLevel recommendation)
    {
        var reviewSuffix = evaluation.ReviewerApproved ? string.Empty : " (flagged by reviewer)";
        var gaps = evaluation.SkillGaps.Where(g => g.Required).Select(g => g.SkillName).ToList();
        var gapText = gaps.Count > 0 ? $" Missing required: {string.Join(", ", gaps)}." : string.Empty;
        return $"Score {evaluation.FitScore} → {recommendation}.{gapText}{reviewSuffix}";
    }
}
