using IRA.Application.Abstractions.Persistence;
using IRA.Application.Common;
using IRA.Application.DTOs;
using IRA.Application.Mapping;

namespace IRA.Application.Features.JobDescriptions;

public record ListJobDescriptionsQuery : IQuery<IReadOnlyList<JobDescriptionDto>>;

public class ListJobDescriptionsQueryHandler : IQueryHandler<ListJobDescriptionsQuery, IReadOnlyList<JobDescriptionDto>>
{
    private readonly ITalentRepository _repository;

    public ListJobDescriptionsQueryHandler(ITalentRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<JobDescriptionDto>> HandleAsync(ListJobDescriptionsQuery query, CancellationToken ct = default)
    {
        var jobs = await _repository.ListJobDescriptionsAsync(ct);
        return jobs.Select(j => j.ToDto()).ToList();
    }
}
