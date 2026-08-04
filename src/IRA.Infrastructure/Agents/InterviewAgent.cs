using System.Text;
using IRA.Application.Abstractions.AI;
using IRA.Application.Abstractions.Agents;
using IRA.Domain.Entities;
using IRA.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace IRA.Infrastructure.Agents;

/// <summary>
/// Interview Agent implementation. Generates role-specific technical, behavioural and
/// situational questions with evaluation criteria. Falls back to skill-targeted templates
/// derived from the candidate's skill gaps and the job's required skills.
/// </summary>
public class InterviewAgent : IInterviewAgent
{
    private const string SystemPrompt =
        "You are an interview preparation agent. Generate role-specific interview questions for the " +
        "candidate covering technical, behavioral and situational categories. For each question include " +
        "evaluation criteria. Respond ONLY as JSON array: " +
        "[{\"text\":string,\"type\":\"Technical|Behavioral|Situational\",\"difficulty\":\"Easy|Medium|Hard\"," +
        "\"evaluationCriteria\":string,\"targetSkill\":string}]. Generate 6-8 questions.";

    private readonly ITextGenerationService _textGeneration;
    private readonly ILogger<InterviewAgent> _logger;

    public InterviewAgent(ITextGenerationService textGeneration, ILogger<InterviewAgent> logger)
    {
        _textGeneration = textGeneration;
        _logger = logger;
    }

    public async Task<InterviewKit> GenerateAsync(
        Candidate candidate,
        JobDescription job,
        CandidateEvaluation evaluation,
        CancellationToken cancellationToken = default)
    {
        var kit = new InterviewKit(candidate.Id, job.Id);

        if (_textGeneration.IsLive)
        {
            try
            {
                var prompt = BuildPrompt(candidate, job, evaluation);
                var response = await _textGeneration.CompleteAsync(SystemPrompt, prompt, cancellationToken);
                var questions = AgentJson.TryDeserialize<List<QuestionDto>>(response);
                if (questions is { Count: > 0 })
                {
                    kit.AddRange(questions.Select(Map));
                    return kit;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Interview agent AI call failed; using deterministic fallback.");
            }
        }

        kit.AddRange(FallbackQuestions(candidate, job));
        return kit;
    }

    internal static IEnumerable<InterviewQuestion> FallbackQuestions(Candidate candidate, JobDescription job)
    {
        var questions = new List<InterviewQuestion>();

        // Technical questions from the job's required skills.
        foreach (var skill in job.RequiredSkills.Take(4))
        {
            questions.Add(new InterviewQuestion
            {
                Text = $"Describe a project where you applied {skill.Name}. What challenges did you face and how did you solve them?",
                Type = QuestionType.Technical,
                Difficulty = DifficultyLevel.Medium,
                TargetSkill = skill.Name,
                EvaluationCriteria = $"Depth of hands-on {skill.Name} experience; problem-solving approach; measurable outcomes."
            });
        }

        // Behavioural.
        questions.Add(new InterviewQuestion
        {
            Text = "Tell me about a time you had to learn a new technology quickly to deliver a project.",
            Type = QuestionType.Behavioral,
            Difficulty = DifficultyLevel.Medium,
            EvaluationCriteria = "Learning agility, initiative, and how they applied the new skill."
        });

        // Situational.
        questions.Add(new InterviewQuestion
        {
            Text = $"You are given a tight deadline for a {job.Title} deliverable but discover a critical defect. How do you proceed?",
            Type = QuestionType.Situational,
            Difficulty = DifficultyLevel.Hard,
            EvaluationCriteria = "Prioritisation, stakeholder communication, and quality vs. speed trade-offs."
        });

        return questions;
    }

    private static InterviewQuestion Map(QuestionDto dto) => new()
    {
        Text = dto.Text,
        Type = Enum.TryParse<QuestionType>(dto.Type, true, out var t) ? t : QuestionType.Technical,
        Difficulty = Enum.TryParse<DifficultyLevel>(dto.Difficulty, true, out var d) ? d : DifficultyLevel.Medium,
        EvaluationCriteria = dto.EvaluationCriteria,
        TargetSkill = dto.TargetSkill
    };

    private static string BuildPrompt(Candidate candidate, JobDescription job, CandidateEvaluation evaluation)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"ROLE: {job.Title}");
        sb.AppendLine($"REQUIRED SKILLS: {string.Join(", ", job.RequiredSkills.Select(s => s.Name))}");
        sb.AppendLine($"CANDIDATE: {candidate.FullName}");
        sb.AppendLine($"CANDIDATE SKILLS: {string.Join(", ", candidate.Skills.Select(s => s.Name))}");
        sb.AppendLine($"SKILL GAPS: {string.Join(", ", evaluation.SkillGaps.Select(g => g.SkillName))}");
        sb.AppendLine($"FIT SCORE: {evaluation.FitScore}");
        return sb.ToString();
    }

    private record QuestionDto(string Text, string Type, string Difficulty, string? EvaluationCriteria, string? TargetSkill);
}
