using System.Net;
using IRA.Application.Abstractions.Persistence;
using IRA.Domain.Entities;
using Microsoft.Azure.Cosmos;

namespace IRA.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Durable <see cref="ITalentRepository"/> backed by Azure Cosmos DB. Data survives API
/// restarts. Registered in place of the in-memory repository whenever Cosmos is configured.
/// </summary>
public sealed class CosmosTalentRepository : ITalentRepository
{
    private readonly CosmosContext _context;

    public CosmosTalentRepository(CosmosContext context) => _context = context;

    // ----- Candidates (partitioned by id) -----

    public async Task AddCandidateAsync(Candidate candidate, CancellationToken ct = default)
    {
        var container = await _context.GetContainerAsync(CosmosContext.Candidates, ct);
        var doc = CandidateDocument.From(candidate);
        await container.UpsertItemAsync(doc, new PartitionKey(doc.Id), cancellationToken: ct);
    }

    public async Task<Candidate?> GetCandidateAsync(Guid id, CancellationToken ct = default)
    {
        var container = await _context.GetContainerAsync(CosmosContext.Candidates, ct);
        var doc = await ReadOrDefaultAsync<CandidateDocument>(container, id.ToString(), id.ToString(), ct);
        return doc?.ToDomain();
    }

    public async Task<IReadOnlyList<Candidate>> ListCandidatesAsync(CancellationToken ct = default)
    {
        var container = await _context.GetContainerAsync(CosmosContext.Candidates, ct);
        var docs = await QueryAsync<CandidateDocument>(container, new QueryDefinition("SELECT * FROM c"), null, ct);
        return docs.Select(d => d.ToDomain()).ToList();
    }

    // ----- Resumes (partitioned by id) -----

    public async Task AddResumeAsync(Resume resume, CancellationToken ct = default) => await UpsertResumeAsync(resume, ct);

    public async Task UpdateResumeAsync(Resume resume, CancellationToken ct = default) => await UpsertResumeAsync(resume, ct);

    private async Task UpsertResumeAsync(Resume resume, CancellationToken ct)
    {
        var container = await _context.GetContainerAsync(CosmosContext.Resumes, ct);
        var doc = ResumeDocument.From(resume);
        await container.UpsertItemAsync(doc, new PartitionKey(doc.Id), cancellationToken: ct);
    }

    public async Task<Resume?> GetResumeAsync(Guid id, CancellationToken ct = default)
    {
        var container = await _context.GetContainerAsync(CosmosContext.Resumes, ct);
        var doc = await ReadOrDefaultAsync<ResumeDocument>(container, id.ToString(), id.ToString(), ct);
        return doc?.ToDomain();
    }

    // ----- Job descriptions (partitioned by id) -----

    public async Task AddJobDescriptionAsync(JobDescription jobDescription, CancellationToken ct = default)
    {
        var container = await _context.GetContainerAsync(CosmosContext.Jobs, ct);
        var doc = JobDescriptionDocument.From(jobDescription);
        await container.UpsertItemAsync(doc, new PartitionKey(doc.Id), cancellationToken: ct);
    }

    public async Task<JobDescription?> GetJobDescriptionAsync(Guid id, CancellationToken ct = default)
    {
        var container = await _context.GetContainerAsync(CosmosContext.Jobs, ct);
        var doc = await ReadOrDefaultAsync<JobDescriptionDocument>(container, id.ToString(), id.ToString(), ct);
        return doc?.ToDomain();
    }

    public async Task<IReadOnlyList<JobDescription>> ListJobDescriptionsAsync(CancellationToken ct = default)
    {
        var container = await _context.GetContainerAsync(CosmosContext.Jobs, ct);
        var docs = await QueryAsync<JobDescriptionDocument>(container, new QueryDefinition("SELECT * FROM c"), null, ct);
        return docs.Select(d => d.ToDomain()).ToList();
    }

    // ----- Evaluations (partitioned by jobDescriptionId) -----

    public async Task SaveEvaluationAsync(CandidateEvaluation evaluation, CancellationToken ct = default)
    {
        var container = await _context.GetContainerAsync(CosmosContext.Evaluations, ct);
        var doc = EvaluationDocument.From(evaluation);
        await container.UpsertItemAsync(doc, new PartitionKey(doc.JobDescriptionId.ToString()), cancellationToken: ct);
    }

    public async Task<IReadOnlyList<CandidateEvaluation>> GetEvaluationsForJobAsync(Guid jobDescriptionId, CancellationToken ct = default)
    {
        var container = await _context.GetContainerAsync(CosmosContext.Evaluations, ct);
        var docs = await QueryAsync<EvaluationDocument>(container, new QueryDefinition("SELECT * FROM c"),
            new PartitionKey(jobDescriptionId.ToString()), ct);
        return docs.Select(d => d.ToDomain()).ToList();
    }

    // ----- Interview kits (partitioned by jobDescriptionId) -----

    public async Task SaveInterviewKitAsync(InterviewKit kit, CancellationToken ct = default)
    {
        var container = await _context.GetContainerAsync(CosmosContext.InterviewKits, ct);
        var doc = InterviewKitDocument.From(kit);
        await container.UpsertItemAsync(doc, new PartitionKey(doc.JobDescriptionId.ToString()), cancellationToken: ct);
    }

    public async Task<IReadOnlyList<InterviewKit>> GetInterviewKitsForJobAsync(Guid jobDescriptionId, CancellationToken ct = default)
    {
        var container = await _context.GetContainerAsync(CosmosContext.InterviewKits, ct);
        var docs = await QueryAsync<InterviewKitDocument>(container, new QueryDefinition("SELECT * FROM c"),
            new PartitionKey(jobDescriptionId.ToString()), ct);
        return docs.Select(d => d.ToDomain()).ToList();
    }

    // ----- Rankings (one per job; id == jobDescriptionId) -----

    public async Task SaveRankingAsync(CandidateRanking ranking, CancellationToken ct = default)
    {
        var container = await _context.GetContainerAsync(CosmosContext.Rankings, ct);
        var doc = RankingDocument.From(ranking);
        await container.UpsertItemAsync(doc, new PartitionKey(doc.Id), cancellationToken: ct);
    }

    public async Task<CandidateRanking?> GetRankingForJobAsync(Guid jobDescriptionId, CancellationToken ct = default)
    {
        var container = await _context.GetContainerAsync(CosmosContext.Rankings, ct);
        var doc = await ReadOrDefaultAsync<RankingDocument>(container, jobDescriptionId.ToString(), jobDescriptionId.ToString(), ct);
        return doc?.ToDomain();
    }

    // ----- Helpers -----

    private static async Task<T?> ReadOrDefaultAsync<T>(Container container, string id, string partitionKey, CancellationToken ct)
    {
        try
        {
            var response = await container.ReadItemAsync<T>(id, new PartitionKey(partitionKey), cancellationToken: ct);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
    }

    private static async Task<List<T>> QueryAsync<T>(Container container, QueryDefinition query, PartitionKey? partitionKey, CancellationToken ct)
    {
        var options = partitionKey is { } pk ? new QueryRequestOptions { PartitionKey = pk } : null;
        var results = new List<T>();
        using var iterator = container.GetItemQueryIterator<T>(query, requestOptions: options);
        while (iterator.HasMoreResults)
        {
            foreach (var item in await iterator.ReadNextAsync(ct))
            {
                results.Add(item);
            }
        }

        return results;
    }
}
