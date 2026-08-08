using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IRA.Application.DTOs;

namespace IRA.Web.Services;

/// <summary>
/// Typed HTTP client the MVC frontend uses to talk to the ASP.NET Core Web API backend,
/// matching the case-study flow (MVC → Web API → Semantic Kernel orchestrator). The signed-in
/// user's JWT is read from the auth cookie and forwarded as a bearer token on every call.
/// </summary>
public class RecruitmentApiClient
{
    /// <summary>Claim type under which the API bearer token is stored in the auth cookie.</summary>
    public const string AccessTokenClaim = "access_token";

    private readonly HttpClient _http;
    private readonly ILogger<RecruitmentApiClient> _logger;

    public RecruitmentApiClient(HttpClient http, IHttpContextAccessor httpContext, ILogger<RecruitmentApiClient> logger)
    {
        _http = http;
        _logger = logger;

        var token = httpContext.HttpContext?.User.FindFirst(AccessTokenClaim)?.Value;
        if (!string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // ----- Authentication (anonymous; no bearer token required) -----

    public async Task<AuthResultDto?> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthResultDto>(cancellationToken: ct);
    }

    /// <summary>Registers a candidate. Returns the auth result, or a message on failure.</summary>
    public async Task<(AuthResultDto? Result, string? Error)> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return (await response.Content.ReadFromJsonAsync<AuthResultDto>(cancellationToken: ct), null);
        }

        var problem = await response.Content.ReadFromJsonAsync<MessageResponse>(cancellationToken: ct);
        return (null, problem?.Message ?? "Registration failed.");
    }

    // ----- Resume -----

    public async Task<ResumeUploadResultDto?> UploadResumeAsync(Stream file, string fileName, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(file);
        content.Add(fileContent, "file", fileName);
        var response = await _http.PostAsync("api/resume/upload", content, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ResumeUploadResultDto>(cancellationToken: ct);
    }

    // ----- Candidates -----

    public async Task<IReadOnlyList<CandidateDto>> GetCandidatesAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<CandidateDto>>("api/candidate", ct) ?? Array.Empty<CandidateDto>();

    /// <summary>The signed-in candidate's own profile, or null if they have not uploaded a resume yet.</summary>
    public async Task<CandidateDto?> GetMyProfileAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/candidate/me", ct);
        if (response.StatusCode == HttpStatusCode.NoContent || !response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CandidateDto>(cancellationToken: ct);
    }

    // ----- Job descriptions -----

    public async Task<IReadOnlyList<JobDescriptionDto>> GetJobDescriptionsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<JobDescriptionDto>>("api/jobdescription", ct) ?? Array.Empty<JobDescriptionDto>();

    public async Task<JobDescriptionDto?> CreateJobDescriptionAsync(CreateJobDescriptionDto dto, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/jobdescription", dto, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JobDescriptionDto>(cancellationToken: ct);
    }

    // ----- Matching / ranking / interview -----

    public async Task<RecruitmentEvaluationResultDto?> EvaluateAsync(EvaluateCandidatesRequestDto request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/matching/evaluate", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RecruitmentEvaluationResultDto>(cancellationToken: ct);
    }

    public async Task<CandidateRankingDto?> GetRankingAsync(Guid jobId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/ranking/job/{jobId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CandidateRankingDto>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<InterviewKitDto>> GetInterviewKitsAsync(Guid jobId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<InterviewKitDto>>($"api/interview/job/{jobId}", ct)
        ?? Array.Empty<InterviewKitDto>();

    // ----- Audit -----

    public async Task<IReadOnlyList<AuditEntryDto>> GetAuditAsync(int count = 50, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<IReadOnlyList<AuditEntryDto>>($"api/audit?count={count}", ct)
                   ?? Array.Empty<AuditEntryDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Audit trail unavailable.");
            return Array.Empty<AuditEntryDto>();
        }
    }

    private record MessageResponse(string Message);
}
