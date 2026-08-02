using IRA.Application.Abstractions.Persistence;
using IRA.Application.Common;
using IRA.Application.DTOs;
using IRA.Application.Mapping;

namespace IRA.Application.Features.Candidates;

public record ListCandidatesQuery : IQuery<IReadOnlyList<CandidateDto>>;

public class ListCandidatesQueryHandler : IQueryHandler<ListCandidatesQuery, IReadOnlyList<CandidateDto>>
{
    private readonly ITalentRepository _repository;

    public ListCandidatesQueryHandler(ITalentRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<CandidateDto>> HandleAsync(ListCandidatesQuery query, CancellationToken ct = default)
    {
        var candidates = await _repository.ListCandidatesAsync(ct);
        return candidates.Select(c => c.ToDto()).ToList();
    }
}
