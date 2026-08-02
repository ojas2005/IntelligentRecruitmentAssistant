using IRA.Domain.Entities;
using IRA.Domain.ValueObjects;

namespace IRA.Application.Abstractions.Agents;

/// <summary>
/// Job Matching Agent — compares a candidate profile with a job description,
/// performs semantic matching, computes the fit score and identifies skill gaps.
/// Second/third stage of the flow (Analyze -> Match).
/// </summary>
public interface IJobMatchingAgent
{
    Task<CandidateEvaluation> EvaluateAsync(
        Candidate candidate,
        JobDescription job,
        IReadOnlyList<Citation> ragContext,
        CancellationToken cancellationToken = default);
}
