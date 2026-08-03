using System.Collections.Concurrent;
using IRA.Application.Abstractions.Persistence;
using IRA.Domain.Entities;

namespace IRA.Infrastructure.Persistence;

/// <summary>
/// Centralised talent repository implemented with thread-safe in-memory stores.
/// This keeps the case-study solution self-contained; swap for an EF Core / Cosmos DB
/// implementation of <see cref="ITalentRepository"/> without touching any other layer.
/// </summary>
public class InMemoryTalentRepository : ITalentRepository
{
    private readonly ConcurrentDictionary<Guid, Candidate> _candidates = new();
    private readonly ConcurrentDictionary<Guid, Resume> _resumes = new();
    private readonly ConcurrentDictionary<Guid, JobDescription> _jobs = new();
    private readonly ConcurrentDictionary<Guid, CandidateEvaluation> _evaluations = new();
    private readonly ConcurrentDictionary<Guid, InterviewKit> _interviewKits = new();
    private readonly ConcurrentDictionary<Guid, CandidateRanking> _rankings = new();

    public Task AddCandidateAsync(Candidate candidate, CancellationToken ct = default)
    {
        _candidates[candidate.Id] = candidate;
        return Task.CompletedTask;
    }

    public Task<Candidate?> GetCandidateAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_candidates.GetValueOrDefault(id));

    public Task<IReadOnlyList<Candidate>> ListCandidatesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Candidate>>(_candidates.Values.ToList());

    public Task AddResumeAsync(Resume resume, CancellationToken ct = default)
    {
        _resumes[resume.Id] = resume;
        return Task.CompletedTask;
    }

    public Task UpdateResumeAsync(Resume resume, CancellationToken ct = default)
    {
        _resumes[resume.Id] = resume;
        return Task.CompletedTask;
    }

    public Task<Resume?> GetResumeAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_resumes.GetValueOrDefault(id));

    public Task AddJobDescriptionAsync(JobDescription jobDescription, CancellationToken ct = default)
    {
        _jobs[jobDescription.Id] = jobDescription;
        return Task.CompletedTask;
    }

    public Task<JobDescription?> GetJobDescriptionAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_jobs.GetValueOrDefault(id));

    public Task<IReadOnlyList<JobDescription>> ListJobDescriptionsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<JobDescription>>(_jobs.Values.ToList());

    public Task SaveEvaluationAsync(CandidateEvaluation evaluation, CancellationToken ct = default)
    {
        _evaluations[evaluation.Id] = evaluation;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CandidateEvaluation>> GetEvaluationsForJobAsync(Guid jobDescriptionId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CandidateEvaluation>>(
            _evaluations.Values.Where(e => e.JobDescriptionId == jobDescriptionId).ToList());

    public Task SaveInterviewKitAsync(InterviewKit kit, CancellationToken ct = default)
    {
        _interviewKits[kit.Id] = kit;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<InterviewKit>> GetInterviewKitsForJobAsync(Guid jobDescriptionId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<InterviewKit>>(
            _interviewKits.Values.Where(k => k.JobDescriptionId == jobDescriptionId).ToList());

    public Task SaveRankingAsync(CandidateRanking ranking, CancellationToken ct = default)
    {
        _rankings[ranking.JobDescriptionId] = ranking;
        return Task.CompletedTask;
    }

    public Task<CandidateRanking?> GetRankingForJobAsync(Guid jobDescriptionId, CancellationToken ct = default) =>
        Task.FromResult(_rankings.GetValueOrDefault(jobDescriptionId));
}
