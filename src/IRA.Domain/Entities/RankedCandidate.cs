using IRA.Domain.Enums;
using IRA.Domain.ValueObjects;

namespace IRA.Domain.Entities;

/// <summary>A single candidate's position within a ranking.</summary>
public class RankedCandidate
{
    public Guid CandidateId { get; init; }
    public string CandidateName { get; init; } = string.Empty;
    public int Rank { get; set; }
    public FitScore Score { get; init; }
    public RecommendationLevel Recommendation { get; init; }
    public string Justification { get; init; } = string.Empty;
}
