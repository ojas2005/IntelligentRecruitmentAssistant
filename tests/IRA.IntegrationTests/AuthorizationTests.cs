using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace IRA.IntegrationTests;

/// <summary>Authorization Testing — Entra ID-style policies enforced by the API endpoints.</summary>
public class AuthorizationTests : IClassFixture<RecruitmentApiFactory>
{
    private readonly RecruitmentApiFactory _factory;

    public AuthorizationTests(RecruitmentApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Protected_endpoint_returns_401_when_unauthenticated()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/candidate");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Recruiter_can_access_candidate_endpoint()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/candidate");
        request.Headers.Add("X-Test-User", "recruiter@corp.com");
        request.Headers.Add("X-Test-Roles", "Recruiter");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Recruiter_is_forbidden_from_admin_only_audit_endpoint()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/audit");
        request.Headers.Add("X-Test-User", "recruiter@corp.com");
        request.Headers.Add("X-Test-Roles", "Recruiter");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Administrator_can_access_audit_endpoint()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/audit");
        request.Headers.Add("X-Test-User", "admin@corp.com");
        request.Headers.Add("X-Test-Roles", "RecruitmentAdministrator");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_endpoint_is_anonymous()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
