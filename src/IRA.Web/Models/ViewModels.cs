using IRA.Application.DTOs;

namespace IRA.Web.Models;

public class DashboardViewModel
{
    public IReadOnlyList<CandidateDto> Candidates { get; set; } = Array.Empty<CandidateDto>();
    public IReadOnlyList<JobDescriptionDto> Jobs { get; set; } = Array.Empty<JobDescriptionDto>();
    public bool ApiReachable { get; set; }
    public string? Error { get; set; }
}

public class RankingViewModel
{
    public IReadOnlyList<JobDescriptionDto> Jobs { get; set; } = Array.Empty<JobDescriptionDto>();
    public Guid? SelectedJobId { get; set; }
    public RecruitmentEvaluationResultDto? Result { get; set; }
}
