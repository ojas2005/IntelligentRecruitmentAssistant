using IRA.Application.Abstractions.AI;
using IRA.Application.Abstractions.Agents;
using IRA.Application.Abstractions.Audit;
using IRA.Application.Abstractions.Orchestration;
using IRA.Application.Abstractions.Persistence;
using IRA.Application.Abstractions.Search;
using IRA.Application.DTOs;
using IRA.Application.Mapping;
using IRA.Domain.Entities;
using IRA.Domain.Enums;
using IRA.Domain.Rules;
using Microsoft.Extensions.Logging;

namespace IRA.Application.Services;

/// <summary>
/// AI Agent Coordination Service backing the Semantic Kernel Orchestrator.
///
/// Executes the case-study Core Processing flow in order:
///   Extract -> Analyze -> Match -> Generate Questions -> Rank
/// with RAG grounding on the Match step and Reviewer Agent validation before results
/// are surfaced. Every stage is audit-logged for traceability.
/// </summary>
public class RecruitmentOrchestrator : IRecruitmentOrchestrator
{
    private readonly ITalentRepository _repository;
    private readonly IRagRetrievalService _rag;
    private readonly IJobMatchingAgent _jobMatchingAgent;
    private readonly IInterviewAgent _interviewAgent;
    private readonly IReviewerAgent _reviewerAgent;
    private readonly IRankingAgent _rankingAgent;
    private readonly ITextGenerationService _textGeneration;
    private readonly IAuditLogger _audit;
    private readonly ILogger<RecruitmentOrchestrator> _logger;

    public RecruitmentOrchestrator(
        ITalentRepository repository,
        IRagRetrievalService rag,
        IJobMatchingAgent jobMatchingAgent,
        IInterviewAgent interviewAgent,
        IReviewerAgent reviewerAgent,
        IRankingAgent rankingAgent,
        ITextGenerationService textGeneration,
        IAuditLogger audit,
        ILogger<RecruitmentOrchestrator> logger)
    {
        _repository = repository;
        _rag = rag;
        _jobMatchingAgent = jobMatchingAgent;
        _interviewAgent = interviewAgent;
        _reviewerAgent = reviewerAgent;
        _rankingAgent = rankingAgent;
        _textGeneration = textGeneration;
        _audit = audit;
        _logger = logger;
    }

    public async Task<RecruitmentEvaluationResultDto> RunEvaluationAsync(
        EvaluateCandidatesRequestDto request,
        string actor,
        CancellationToken ct = default)
    {
        var job = await _repository.GetJobDescriptionAsync(request.JobDescriptionId, ct)
                  ?? throw new InvalidOperationException($"Job description {request.JobDescriptionId} was not found.");

        var candidates = await ResolveCandidatesAsync(request, ct);
        _logger.LogInformation("Orchestrating evaluation of {Count} candidates for '{Job}'.", candidates.Count, job.Title);
        await _audit.LogAsync(new AuditEntry(actor, "EvaluationStarted", nameof(JobDescription), job.Id.ToString(),
            $"{candidates.Count} candidates."), ct);

        var evaluations = new List<CandidateEvaluation>();

        // ----- Analyze + Match (per candidate), grounded with RAG, validated by Reviewer -----
        foreach (var candidate in candidates)
        {
            // RAG: retrieve role requirements / competency context to augment the prompt.
            var ragContext = await _rag.RetrieveAsync(
                $"Requirements and competencies for {job.Title}. Candidate skills: {string.Join(", ", candidate.Skills.Select(s => s.Name))}",
                topK: 5,
                categoryFilter: null,
                ct);

            var evaluation = await _jobMatchingAgent.EvaluateAsync(candidate, job, ragContext, ct);

            // Reviewer Agent validates the evaluation for consistency/fairness before it counts.
            var review = await _reviewerAgent.ReviewEvaluationAsync(evaluation, job, ct);
            evaluation.ApplyReview(review.Approved, review.Notes);

            await _repository.SaveEvaluationAsync(evaluation, ct);
            evaluations.Add(evaluation);
        }

        // ----- Rank (aggregate validated evaluations) -----
        var ranking = await _rankingAgent.RankAsync(job, evaluations, ct);

        // Reviewer Agent validates the final ranking before presentation.
        var rankingReview = await _reviewerAgent.ReviewRankingAsync(ranking, ct);
        ranking.MarkReviewed(rankingReview.Approved);
        await _repository.SaveRankingAsync(ranking, ct);

        // ----- Generate Questions for the top shortlist -----
        var interviewKits = new List<InterviewKit>();
        var shortlist = ranking.TopShortlist(Math.Max(0, request.InterviewShortlistSize)).ToList();
        foreach (var ranked in shortlist)
        {
            var candidate = candidates.First(c => c.Id == ranked.CandidateId);
            var evaluation = evaluations.First(e => e.CandidateId == ranked.CandidateId);
            var kit = await _interviewAgent.GenerateAsync(candidate, job, evaluation, ct);
            await _repository.SaveInterviewKitAsync(kit, ct);
            interviewKits.Add(kit);
        }

        await _audit.LogAsync(new AuditEntry(actor, "EvaluationCompleted", nameof(CandidateRanking), ranking.Id.ToString(),
            $"Ranked {ranking.Candidates.Count} candidates; generated {interviewKits.Count} interview kits."), ct);

        return new RecruitmentEvaluationResultDto
        {
            Ranking = ranking.ToDto(),
            Evaluations = evaluations.Select(e => e.ToDto()).ToList(),
            InterviewKits = interviewKits.Select(k => k.ToDto()).ToList(),
            UsedAiFallback = !_textGeneration.IsLive
        };
    }

    private async Task<IReadOnlyList<Candidate>> ResolveCandidatesAsync(
        EvaluateCandidatesRequestDto request,
        CancellationToken ct)
    {
        if (request.CandidateIds.Count == 0)
        {
            return await _repository.ListCandidatesAsync(ct);
        }

        var resolved = new List<Candidate>();
        foreach (var id in request.CandidateIds)
        {
            var candidate = await _repository.GetCandidateAsync(id, ct);
            if (candidate is not null)
            {
                resolved.Add(candidate);
            }
        }

        return resolved;
    }
}
