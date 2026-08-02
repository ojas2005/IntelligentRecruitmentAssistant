using IRA.Application.Abstractions.AI;
using IRA.Application.Abstractions.Agents;
using IRA.Application.Abstractions.Audit;
using IRA.Application.Abstractions.Documents;
using IRA.Application.Abstractions.Persistence;
using IRA.Application.Abstractions.Search;
using IRA.Application.Abstractions.Storage;
using IRA.Application.DTOs;
using IRA.Domain.Entities;
using IRA.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace IRA.Application.Services;

/// <summary>
/// Resume Processing Workflow — implements the Document Ingestion pipeline:
/// Upload -> Extract content -> Parse candidate info -> Generate embeddings -> Store in vector DB.
/// </summary>
public class ResumeProcessingService
{
    private readonly IBlobStorage _blobStorage;
    private readonly IDocumentExtractor _extractor;
    private readonly IResumeParserAgent _parserAgent;
    private readonly IEmbeddingGenerator _embeddings;
    private readonly IVectorStore _vectorStore;
    private readonly ITalentRepository _repository;
    private readonly IAuditLogger _audit;
    private readonly ILogger<ResumeProcessingService> _logger;

    public ResumeProcessingService(
        IBlobStorage blobStorage,
        IDocumentExtractor extractor,
        IResumeParserAgent parserAgent,
        IEmbeddingGenerator embeddings,
        IVectorStore vectorStore,
        ITalentRepository repository,
        IAuditLogger audit,
        ILogger<ResumeProcessingService> logger)
    {
        _blobStorage = blobStorage;
        _extractor = extractor;
        _parserAgent = parserAgent;
        _embeddings = embeddings;
        _vectorStore = vectorStore;
        _repository = repository;
        _audit = audit;
        _logger = logger;
    }

    public async Task<ResumeUploadResultDto> ProcessAsync(
        Stream fileStream,
        string fileName,
        string actor,
        CancellationToken ct = default)
    {
        // 1. Persist the raw file (Azure Blob Storage).
        using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer, ct);
        buffer.Position = 0;
        var blobPath = await _blobStorage.UploadAsync(buffer, fileName, "resumes", ct);

        var resume = new Resume(fileName, blobPath);
        await _repository.AddResumeAsync(resume, ct);

        try
        {
            // 2. Extract text (Azure Document Intelligence).
            buffer.Position = 0;
            var text = await _extractor.ExtractTextAsync(buffer, fileName, ct);
            resume.MarkExtracted(text);

            // 3. Parse candidate information (Resume Parser Agent).
            var parsed = await _parserAgent.ParseAsync(text, ct);
            var candidate = BuildCandidate(parsed);
            await _repository.AddCandidateAsync(candidate, ct);
            resume.MarkParsed(candidate.Id);

            // 4. Generate embeddings and 5. store in the vector database with metadata.
            var embedding = await _embeddings.GenerateAsync(text, ct);
            await _vectorStore.UpsertAsync(new VectorRecord
            {
                Id = candidate.Id.ToString("N"),
                Content = text,
                SourceName = $"Resume: {candidate.FullName}",
                Category = DocumentCategory.CandidateResume,
                Embedding = embedding,
                Metadata = new Dictionary<string, string>
                {
                    ["candidateId"] = candidate.Id.ToString(),
                    ["candidateName"] = candidate.FullName,
                    ["location"] = candidate.Location ?? string.Empty,
                    ["experience"] = candidate.TotalYearsExperience.ToString("0.#"),
                    ["skills"] = string.Join(", ", candidate.Skills.Select(s => s.Name))
                }
            }, ct);
            resume.MarkEmbedded();

            await _repository.UpdateResumeAsync(resume, ct);
            await _audit.LogAsync(new AuditEntry(actor, "ResumeUploaded", nameof(Resume), resume.Id.ToString(),
                $"Parsed candidate '{candidate.FullName}' with {candidate.Skills.Count} skills."), ct);

            return new ResumeUploadResultDto(resume.Id, candidate.Id, candidate.FullName, resume.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resume processing failed for {FileName}", fileName);
            resume.MarkFailed(ex.Message);
            await _repository.UpdateResumeAsync(resume, ct);
            await _audit.LogAsync(new AuditEntry(actor, "ResumeUploadFailed", nameof(Resume), resume.Id.ToString(), ex.Message), ct);
            throw;
        }
    }

    private static Candidate BuildCandidate(ParsedResumeDto parsed)
    {
        var candidate = new Candidate(
            string.IsNullOrWhiteSpace(parsed.FullName) ? "Unknown Candidate" : parsed.FullName,
            parsed.Email,
            parsed.Phone,
            parsed.Location);

        foreach (var skill in parsed.Skills)
        {
            candidate.AddSkill(new Skill(skill.Name, skill.Category, skill.YearsOfExperience));
        }

        foreach (var exp in parsed.Experiences)
        {
            candidate.AddExperience(new Experience
            {
                Company = exp.Company,
                Role = exp.Role,
                Years = exp.Years,
                Description = exp.Description
            });
        }

        foreach (var edu in parsed.Education)
        {
            candidate.AddEducation(new Education
            {
                Institution = edu.Institution,
                Degree = edu.Degree,
                FieldOfStudy = edu.FieldOfStudy,
                GraduationYear = edu.GraduationYear
            });
        }

        foreach (var cert in parsed.Certifications)
        {
            candidate.AddCertification(new Certification { Name = cert.Name, Issuer = cert.Issuer, Year = cert.Year });
        }

        // Honour an aggregate experience figure when the parser did not itemise experiences.
        candidate.SetTotalYearsExperience(parsed.TotalYearsExperience);

        return candidate;
    }
}
