using IRA.Domain.Common;

namespace IRA.Domain.Entities;

/// <summary>
/// The ordered shortlist produced by the Ranking Agent for a given job description.
/// Business rule: candidates are always ordered by descending score and assigned a 1-based rank.
/// </summary>
public class CandidateRanking : Entity
{
    private readonly List<RankedCandidate> _candidates = new();

    public Guid JobDescriptionId { get; private set; }
    public string JobTitle { get; private set; }
    public bool ReviewerApproved { get; private set; }

    public IReadOnlyList<RankedCandidate> Candidates => _candidates;

    public CandidateRanking(Guid jobDescriptionId, string jobTitle)
    {
        JobDescriptionId = jobDescriptionId;
        JobTitle = jobTitle;
    }

    private CandidateRanking() => JobTitle = string.Empty;

    /// <summary>
    /// Adds candidates and re-sorts the ranking by descending score, reassigning 1-based ranks.
    /// </summary>
    public void SetCandidates(IEnumerable<RankedCandidate> candidates)
    {
        _candidates.Clear();
        _candidates.AddRange(candidates);
        Reorder();
    }

    public void MarkReviewed(bool approved) => ReviewerApproved = approved;

    private void Reorder()
    {
        var ordered = _candidates.OrderByDescending(c => c.Score.Value).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].Rank = i + 1;
        }

        _candidates.Clear();
        _candidates.AddRange(ordered);
    }

    /// <summary>Returns the top <paramref name="count"/> shortlisted candidates.</summary>
    public IEnumerable<RankedCandidate> TopShortlist(int count) => _candidates.Take(count);
}
