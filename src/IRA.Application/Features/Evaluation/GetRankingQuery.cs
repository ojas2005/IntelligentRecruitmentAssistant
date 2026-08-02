using IRA.Application.Abstractions.Persistence;
using IRA.Application.Common;
using IRA.Application.DTOs;
using IRA.Application.Mapping;

namespace IRA.Application.Features.Evaluation;

public record GetRankingQuery(Guid JobDescriptionId) : IQuery<CandidateRankingDto?>;

public class GetRankingQueryHandler : IQueryHandler<GetRankingQuery, CandidateRankingDto?>
{
    private readonly ITalentRepository _repository;

    public GetRankingQueryHandler(ITalentRepository repository) => _repository = repository;

    public async Task<CandidateRankingDto?> HandleAsync(GetRankingQuery query, CancellationToken ct = default)
    {
        var ranking = await _repository.GetRankingForJobAsync(query.JobDescriptionId, ct);
        return ranking?.ToDto();
    }
}
