using IRA.Domain.Entities;

namespace IRA.Application.Abstractions.Agents;

/// <summary>
/// Ranking Agent — aggregates validated evaluations, assigns recommendation scores
/// and produces the prioritised candidate shortlist. Final stage (Rank).
/// </summary>
public interface IRankingAgent
{
    Task<CandidateRanking> RankAsync(
        JobDescription job,
        IReadOnlyList<CandidateEvaluation> evaluations,
        CancellationToken cancellationToken = default);
}
