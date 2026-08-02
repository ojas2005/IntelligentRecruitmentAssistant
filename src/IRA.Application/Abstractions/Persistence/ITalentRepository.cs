using IRA.Domain.Entities;

namespace IRA.Application.Abstractions.Persistence;

/// <summary>
/// The centralised talent repository — persistence for candidates, resumes,
/// job descriptions, evaluations, interview kits and rankings.
/// </summary>
public interface ITalentRepository
{
    // Candidates
    Task AddCandidateAsync(Candidate candidate, CancellationToken ct = default);
    Task<Candidate?> GetCandidateAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Candidate>> ListCandidatesAsync(CancellationToken ct = default);

    // Resumes
    Task AddResumeAsync(Resume resume, CancellationToken ct = default);
    Task UpdateResumeAsync(Resume resume, CancellationToken ct = default);
    Task<Resume?> GetResumeAsync(Guid id, CancellationToken ct = default);

    // Job descriptions
    Task AddJobDescriptionAsync(JobDescription jobDescription, CancellationToken ct = default);
    Task<JobDescription?> GetJobDescriptionAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<JobDescription>> ListJobDescriptionsAsync(CancellationToken ct = default);

    // Evaluations
    Task SaveEvaluationAsync(CandidateEvaluation evaluation, CancellationToken ct = default);
    Task<IReadOnlyList<CandidateEvaluation>> GetEvaluationsForJobAsync(Guid jobDescriptionId, CancellationToken ct = default);

    // Interview kits
    Task SaveInterviewKitAsync(InterviewKit kit, CancellationToken ct = default);
    Task<IReadOnlyList<InterviewKit>> GetInterviewKitsForJobAsync(Guid jobDescriptionId, CancellationToken ct = default);

    // Rankings
    Task SaveRankingAsync(CandidateRanking ranking, CancellationToken ct = default);
    Task<CandidateRanking?> GetRankingForJobAsync(Guid jobDescriptionId, CancellationToken ct = default);
}
