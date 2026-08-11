using System.Text;
using IRA.Application.DTOs;
using IRA.Application.Services;
using IRA.Infrastructure.Agents;
using Xunit;

namespace IRA.UnitTests;

/// <summary>Resume Parsing Testing — extraction of candidate attributes from raw text.</summary>
public class ResumeParsingTests
{
    private const string SampleResume = """
        Alice Johnson
        alice.johnson@example.com
        +1 415 555 0199

        Summary: Senior software engineer with 8 years of experience.

        Skills: C#, ASP.NET Core, Azure, Docker, Kubernetes, SQL

        Experience:
        Contoso - Senior Engineer
        """;

    [Fact]
    public void Fallback_parser_extracts_name_email_phone_and_skills()
    {
        var parsed = ResumeParserAgent.FallbackParse(SampleResume);

        Assert.Equal("Alice Johnson", parsed.FullName);
        Assert.Equal("alice.johnson@example.com", parsed.Email);
        Assert.False(string.IsNullOrWhiteSpace(parsed.Phone));
        Assert.Contains(parsed.Skills, s => s.Name == "C#");
        Assert.Contains(parsed.Skills, s => s.Name == "ASP.NET Core");
        Assert.Contains(parsed.Skills, s => s.Name == "Kubernetes");
        Assert.Equal(8, parsed.TotalYearsExperience);
    }

    [Fact]
    public async Task ResumeProcessingService_produces_a_candidate_and_indexes_it()
    {
        var provider = TestFactory.CreateProvider();
        var service = provider.GetRequiredService<ResumeProcessingService>();

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(SampleResume));
        var result = await service.ProcessAsync(stream, "alice.txt", "tester");

        Assert.Equal("Alice Johnson", result.CandidateName);
        Assert.NotEqual(Guid.Empty, result.CandidateId);

        var repo = provider.GetRequiredService<IRA.Application.Abstractions.Persistence.ITalentRepository>();
        var candidate = await repo.GetCandidateAsync(result.CandidateId);
        Assert.NotNull(candidate);
        Assert.NotEmpty(candidate!.Skills);
        // Aggregate years from the resume ("8 years") must be carried onto the candidate.
        Assert.Equal(8, candidate.TotalYearsExperience);
    }
}
