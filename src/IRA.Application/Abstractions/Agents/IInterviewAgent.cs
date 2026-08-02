using IRA.Domain.Entities;

namespace IRA.Application.Abstractions.Agents;

/// <summary>
/// Interview Agent — generates role-specific technical, behavioural and situational
/// interview questions with evaluation criteria. Fourth stage (Generate Questions).
/// </summary>
public interface IInterviewAgent
{
    Task<InterviewKit> GenerateAsync(
        Candidate candidate,
        JobDescription job,
        CandidateEvaluation evaluation,
        CancellationToken cancellationToken = default);
}
