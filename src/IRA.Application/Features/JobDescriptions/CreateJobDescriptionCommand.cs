using IRA.Application.Common;
using IRA.Application.DTOs;
using IRA.Application.Services;

namespace IRA.Application.Features.JobDescriptions;

public record CreateJobDescriptionCommand(CreateJobDescriptionDto Data, string Actor) : ICommand<JobDescriptionDto>;

public class CreateJobDescriptionCommandHandler : ICommandHandler<CreateJobDescriptionCommand, JobDescriptionDto>
{
    private readonly JobDescriptionService _service;

    public CreateJobDescriptionCommandHandler(JobDescriptionService service) => _service = service;

    public Task<JobDescriptionDto> HandleAsync(CreateJobDescriptionCommand command, CancellationToken ct = default) =>
        _service.CreateAsync(command.Data, command.Actor, ct);
}
