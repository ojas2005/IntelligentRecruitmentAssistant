using IRA.Application.Abstractions.Agents;
using IRA.Domain.Entities;
using IRA.Domain.Enums;
using IRA.Domain.ValueObjects;
using Xunit;

namespace IRA.UnitTests;

/// <summary>Interview Question Generation Testing — the Interview Agent's output.</summary>
public class InterviewQuestionGenerationTests
{
    [Fact]
    public async Task Generates_technical_behavioural_and_situational_questions()
    {
        var provider = TestFactory.CreateProvider();
        var agent = provider.GetRequiredService<IInterviewAgent>();

        var candidate = new Candidate("Dana Lee");
        candidate.AddSkill(new Skill("C#"));

        var job = new JobDescription("Backend Engineer", "desc");
        job.AddRequiredSkill(new Skill("C#"));
        job.AddRequiredSkill(new Skill("Azure"));

        var evaluation = new CandidateEvaluation(candidate.Id, candidate.FullName, job.Id, new FitScore(72));
        evaluation.SetNarrative("summary", "reasoning");

        var kit = await agent.GenerateAsync(candidate, job, evaluation);

        Assert.NotEmpty(kit.Questions);
        Assert.Contains(kit.Questions, q => q.Type == QuestionType.Technical);
        Assert.Contains(kit.Questions, q => q.Type == QuestionType.Behavioral);
        Assert.Contains(kit.Questions, q => q.Type == QuestionType.Situational);
        Assert.All(kit.Questions, q => Assert.False(string.IsNullOrWhiteSpace(q.Text)));
    }
}
