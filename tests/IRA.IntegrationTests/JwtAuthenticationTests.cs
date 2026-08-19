using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using IRA.Application.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IRA.IntegrationTests;

/// <summary>
/// Exercises the real self-issued JWT pipeline end-to-end (login → bearer token → role-gated
/// endpoints), with no test-auth override. Verifies the recruiter and candidate ("user") splits.
/// </summary>
public class JwtAuthenticationTests : IClassFixture<JwtAuthenticationTests.JwtApiFactory>
{
    private readonly JwtApiFactory _factory;

    public JwtAuthenticationTests(JwtApiFactory factory) => _factory = factory;

    /// <summary>Real Program with the JWT bearer scheme active (no header-auth override).</summary>
    public class JwtApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");
    }

    private async Task<AuthResultDto> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(username, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_a_token_and_roles()
    {
        var client = _factory.CreateClient();

        var auth = await LoginAsync(client, "recruiter", "Passw0rd!");

        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        Assert.Contains("Recruiter", auth.Roles);
    }

    [Fact]
    public async Task Login_with_wrong_password_is_unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto("recruiter", "wrong"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_requires_a_bearer_token()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/candidate");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Recruiter_token_can_list_candidates()
    {
        var client = _factory.CreateClient();
        var auth = await LoginAsync(client, "recruiter", "Passw0rd!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var response = await client.GetAsync("/api/candidate");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Candidate_token_is_forbidden_from_recruiter_and_admin_endpoints()
    {
        var client = _factory.CreateClient();
        var auth = await LoginAsync(client, "candidate", "Passw0rd!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var candidateList = await client.GetAsync("/api/candidate");   // recruiter-only
        var audit = await client.GetAsync("/api/audit");               // admin-only

        Assert.Equal(HttpStatusCode.Forbidden, candidateList.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, audit.StatusCode);
    }

    [Fact]
    public async Task Registered_candidate_can_upload_and_read_own_profile()
    {
        var client = _factory.CreateClient();

        // Self-service registration issues a Candidate token.
        var username = $"applicant_{Guid.NewGuid():N}";
        var register = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequestDto(username, "Passw0rd!", "Applicant Test", "applicant@example.com"));
        register.EnsureSuccessStatusCode();
        var auth = (await register.Content.ReadFromJsonAsync<AuthResultDto>())!;
        Assert.Contains("Candidate", auth.Roles);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        // Before upload there is no linked profile.
        var before = await client.GetAsync("/api/candidate/me");
        Assert.Equal(HttpStatusCode.NoContent, before.StatusCode);

        // Candidate uploads their own resume.
        const string resume = "Applicant Test\napplicant@example.com\nSkills: C#, Azure, SQL\n8 years of experience.";
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(resume));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", "applicant.txt");
        var upload = await client.PostAsync("/api/resume/upload", form);
        upload.EnsureSuccessStatusCode();

        // Now the profile is linked and readable.
        var after = await client.GetAsync("/api/candidate/me");
        after.EnsureSuccessStatusCode();
        var profile = await after.Content.ReadFromJsonAsync<CandidateDto>();
        Assert.NotNull(profile);
        Assert.Equal("Applicant Test", profile!.FullName);
        Assert.Contains("C#", profile.Skills);
    }
}
