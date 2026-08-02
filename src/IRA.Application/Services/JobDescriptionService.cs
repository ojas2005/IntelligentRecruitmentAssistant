using IRA.Application.Abstractions.AI;
using IRA.Application.Abstractions.Audit;
using IRA.Application.Abstractions.Persistence;
using IRA.Application.Abstractions.Search;
using IRA.Application.DTOs;
using IRA.Domain.Entities;
using IRA.Domain.Enums;

namespace IRA.Application.Services;

/// <summary>
/// Registers job descriptions and ingests them into the vector store so they can
/// ground candidate matching via RAG.
/// </summary>
public class JobDescriptionService
{
    private readonly ITalentRepository _repository;
    private readonly IEmbeddingGenerator _embeddings;
    private readonly IVectorStore _vectorStore;
    private readonly IAuditLogger _audit;

    public JobDescriptionService(
        ITalentRepository repository,
        IEmbeddingGenerator embeddings,
        IVectorStore vectorStore,
        IAuditLogger audit)
    {
        _repository = repository;
        _embeddings = embeddings;
        _vectorStore = vectorStore;
        _audit = audit;
    }

    public async Task<JobDescriptionDto> CreateAsync(CreateJobDescriptionDto dto, string actor, CancellationToken ct = default)
    {
        var job = new JobDescription(dto.Title, dto.RawText, dto.Department, dto.MinYearsExperience);
        foreach (var s in dto.RequiredSkills)
        {
            job.AddRequiredSkill(new Skill(s));
        }

        foreach (var s in dto.PreferredSkills)
        {
            job.AddPreferredSkill(new Skill(s));
        }

        await _repository.AddJobDescriptionAsync(job, ct);

        // Ingest into the vector store for RAG grounding.
        var embedding = await _embeddings.GenerateAsync($"{job.Title}\n{job.RawText}", ct);
        await _vectorStore.UpsertAsync(new VectorRecord
        {
            Id = job.Id.ToString("N"),
            Content = job.RawText,
            SourceName = $"Job Description: {job.Title}",
            Category = DocumentCategory.JobDescription,
            Embedding = embedding,
            Metadata = new Dictionary<string, string>
            {
                ["jobId"] = job.Id.ToString(),
                ["title"] = job.Title,
                ["department"] = job.Department ?? string.Empty,
                ["requiredSkills"] = string.Join(", ", job.RequiredSkills.Select(s => s.Name))
            }
        }, ct);

        await _audit.LogAsync(new AuditEntry(actor, "JobDescriptionCreated", nameof(JobDescription), job.Id.ToString(), job.Title), ct);

        return new JobDescriptionDto(job.Id, job.Title, job.Department, job.MinYearsExperience,
            job.RequiredSkills.Select(s => s.Name).ToList(),
            job.PreferredSkills.Select(s => s.Name).ToList());
    }
}
