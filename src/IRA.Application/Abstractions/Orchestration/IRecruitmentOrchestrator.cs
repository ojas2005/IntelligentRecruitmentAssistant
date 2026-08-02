using IRA.Application.DTOs;

namespace IRA.Application.Abstractions.Orchestration;

/// <summary>
/// The Semantic Kernel Orchestrator contract. Coordinates the specialised AI agents
/// through the recruitment flow: Extract -> Analyze -> Match -> Generate Questions -> Rank,
/// with Reviewer validation and RAG grounding at each relevant stage.
/// </summary>
public interface IRecruitmentOrchestrator
{
    Task<RecruitmentEvaluationResultDto> RunEvaluationAsync(
        EvaluateCandidatesRequestDto request,
        string actor,
        CancellationToken cancellationToken = default);
}
