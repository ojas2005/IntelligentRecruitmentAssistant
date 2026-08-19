using System.Net;
using System.Net.Http.Json;
using System.Text;
using IRA.Application.DTOs;
using Xunit;

namespace IRA.IntegrationTests;

/// <summary>
/// Integration Testing — drives the full recruitment flow through the HTTP API:
/// upload resumes → create JD → evaluate (Extract→Analyze→Match→Generate→Rank) → read ranking.
/// </summary>
public class RecruitmentFlowIntegrationTests : IClassFixture<RecruitmentApiFactory>
{
    private readonly RecruitmentApiFactory _factory;

    public RecruitmentFlowIntegrationTests(RecruitmentApiFactory factory) => _factory = factory;

    private HttpClient CreateRecruiterClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "recruiter@corp.com");
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Recruiter,RecruitmentAdministrator");
        return client;
    }

    [Fact]
    public async Task End_to_end_flow_produces_a_ranking_and_audit_trail()
    {
        var client = CreateRecruiterClient();

        // 1. Upload two resumes.
        await UploadResumeAsync(client, "grace.txt", """
            Grace Hopper
            grace@example.com
            10 years experience.
            Skills: C#, ASP.NET Core, Azure, Docker
            """);
        await UploadResumeAsync(client, "pat.txt", """
            Pat Novice
            pat@example.com
            1 years experience.
            Skills: HTML
            """);

        // 2. Create a job description.
        var jdResponse = await client.PostAsJsonAsync("/api/jobdescription", new CreateJobDescriptionDto
        {
            Title = "Senior Backend Engineer",
            RawText = "Senior engineer with C#, ASP.NET Core and Azure.",
            MinYearsExperience = 5,
            RequiredSkills = new List<string> { "C#", "ASP.NET Core", "Azure" },
            PreferredSkills = new List<string> { "Docker" }
        });
        jdResponse.EnsureSuccessStatusCode();
        var jd = await jdResponse.Content.ReadFromJsonAsync<JobDescriptionDto>();
        Assert.NotNull(jd);

        // 3. Run the orchestrated evaluation.
        var evalResponse = await client.PostAsJsonAsync("/api/matching/evaluate", new EvaluateCandidatesRequestDto
        {
            JobDescriptionId = jd!.Id,
            InterviewShortlistSize = 1
        });
        evalResponse.EnsureSuccessStatusCode();
        var result = await evalResponse.Content.ReadFromJsonAsync<RecruitmentEvaluationResultDto>();

        Assert.NotNull(result);
        Assert.Equal(2, result!.Ranking.Candidates.Count);
        Assert.Equal("Grace Hopper", result.Ranking.Candidates[0].CandidateName);
        Assert.Single(result.InterviewKits);

        // 4. Read back the ranking via the Ranking API.
        var ranking = await client.GetFromJsonAsync<CandidateRankingDto>($"/api/ranking/job/{jd.Id}");
        Assert.NotNull(ranking);
        Assert.Equal(2, ranking!.Candidates.Count);

        // 5. Audit trail captured the workflow.
        var audit = await client.GetFromJsonAsync<List<AuditEntryDto>>("/api/audit");
        Assert.NotNull(audit);
        Assert.Contains(audit!, e => e.Action == "EvaluationCompleted");
    }

    [Fact]
    public async Task Ranking_for_unknown_job_returns_404()
    {
        var client = CreateRecruiterClient();
        var response = await client.GetAsync($"/api/ranking/job/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task UploadResumeAsync(HttpClient client, string fileName, string text)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(text));
        content.Add(fileContent, "file", fileName);
        var response = await client.PostAsync("/api/resume/upload", content);
        response.EnsureSuccessStatusCode();
    }
}
