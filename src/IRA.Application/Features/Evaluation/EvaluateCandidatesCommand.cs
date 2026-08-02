using IRA.Application.Abstractions.Orchestration;
using IRA.Application.Common;
using IRA.Application.DTOs;

namespace IRA.Application.Features.Evaluation;

public record EvaluateCandidatesCommand(EvaluateCandidatesRequestDto Request, string Actor)
    : ICommand<RecruitmentEvaluationResultDto>;

public class EvaluateCandidatesCommandHandler : ICommandHandler<EvaluateCandidatesCommand, RecruitmentEvaluationResultDto>
{
    private readonly IRecruitmentOrchestrator _orchestrator;

    public EvaluateCandidatesCommandHandler(IRecruitmentOrchestrator orchestrator) => _orchestrator = orchestrator;

    public Task<RecruitmentEvaluationResultDto> HandleAsync(EvaluateCandidatesCommand command, CancellationToken ct = default) =>
        _orchestrator.RunEvaluationAsync(command.Request, command.Actor, ct);
}
