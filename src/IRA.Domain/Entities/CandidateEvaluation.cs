using IRA.Domain.Common;
using IRA.Domain.ValueObjects;

namespace IRA.Domain.Entities;

/// <summary>
/// The result of evaluating one candidate against one job description.
/// Produced by the Job Matching Agent and grounded via RAG citations.
/// </summary>
public class CandidateEvaluation : Entity
{
    private readonly List<SkillGap> _skillGaps = new();
    private readonly List<Citation> _citations = new();
    private readonly List<string> _matchedSkills = new();

    public Guid CandidateId { get; private set; }
    public string CandidateName { get; private set; }
    public Guid JobDescriptionId { get; private set; }
    public FitScore FitScore { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string Reasoning { get; private set; } = string.Empty;
    public bool ReviewerApproved { get; private set; }
    public string? ReviewerNotes { get; private set; }

    public IReadOnlyCollection<SkillGap> SkillGaps => _skillGaps;
    public IReadOnlyCollection<Citation> Citations => _citations;
    public IReadOnlyCollection<string> MatchedSkills => _matchedSkills;

    public CandidateEvaluation(Guid candidateId, string candidateName, Guid jobDescriptionId, FitScore fitScore)
    {
        CandidateId = candidateId;
        CandidateName = candidateName;
        JobDescriptionId = jobDescriptionId;
        FitScore = fitScore;
    }

    private CandidateEvaluation() => CandidateName = string.Empty;

    public void SetNarrative(string summary, string reasoning)
    {
        Summary = summary;
        Reasoning = reasoning;
    }

    public void AddSkillGap(SkillGap gap) => _skillGaps.Add(gap);

    public void AddMatchedSkill(string skill) => _matchedSkills.Add(skill);

    public void AddCitation(Citation citation) => _citations.Add(citation);

    public void AddCitations(IEnumerable<Citation> citations) => _citations.AddRange(citations);

    /// <summary>Applied by the Reviewer Agent after validating the evaluation.</summary>
    public void ApplyReview(bool approved, string? notes)
    {
        ReviewerApproved = approved;
        ReviewerNotes = notes;
    }
}
