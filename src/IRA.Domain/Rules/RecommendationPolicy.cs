using IRA.Domain.Enums;
using IRA.Domain.ValueObjects;

namespace IRA.Domain.Rules;

/// <summary>
/// Domain business rule that maps a numeric fit score to a hiring recommendation.
/// Centralised here so ranking, matching and reporting stay consistent.
/// </summary>
public static class RecommendationPolicy
{
    public static RecommendationLevel FromScore(FitScore score) => score.Value switch
    {
        >= 85 => RecommendationLevel.StrongShortlist,
        >= 70 => RecommendationLevel.Shortlist,
        >= 50 => RecommendationLevel.Consider,
        >= 30 => RecommendationLevel.Reject,
        _ => RecommendationLevel.StrongReject
    };

    /// <summary>Candidates at or above this recommendation are considered shortlisted.</summary>
    public static bool IsShortlisted(RecommendationLevel level) =>
        level is RecommendationLevel.Shortlist or RecommendationLevel.StrongShortlist;
}
