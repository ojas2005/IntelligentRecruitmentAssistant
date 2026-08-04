using System.Text;
using IRA.Application.Abstractions.AI;
using IRA.Application.Abstractions.Agents;
using IRA.Domain.Entities;
using IRA.Domain.Rules;
using IRA.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace IRA.Infrastructure.Agents;

/// <summary>
/// Job Matching Agent implementation. Computes a grounded fit score and skill gaps with
/// deterministic domain rules, then (when available) asks Azure OpenAI to produce a
/// recruiter-facing summary and reasoning grounded in the retrieved RAG context.
/// </summary>
public class JobMatchingAgent : IJobMatchingAgent
{
    private const string SystemPrompt =
        "You are a candidate-job matching agent. Using ONLY the provided candidate profile, " +
        "job description and retrieved context, write a concise recruiter summary (2-3 sentences) " +
        "and the reasoning behind the fit. Respond as JSON: {\"summary\":string,\"reasoning\":string}. " +
        "Do not invent skills or requirements that are not present in the context.";

    private readonly ITextGenerationService _textGeneration;
    private readonly ILogger<JobMatchingAgent> _logger;

    public JobMatchingAgent(ITextGenerationService textGeneration, ILogger<JobMatchingAgent> logger)
    {
        _textGeneration = textGeneration;
        _logger = logger;
    }

    public async Task<CandidateEvaluation> EvaluateAsync(
        Candidate candidate,
        JobDescription job,
        IReadOnlyList<Citation> ragContext,
        CancellationToken cancellationToken = default)
    {
        // Deterministic grounding signal (also the fallback score).
        var fitScore = SkillMatchingRules.ComputeFitScore(candidate, job);
        var gaps = SkillMatchingRules.IdentifySkillGaps(candidate, job);
        var matched = SkillMatchingRules.MatchedSkills(candidate, job);

        var evaluation = new CandidateEvaluation(candidate.Id, candidate.FullName, job.Id, fitScore);
        foreach (var gap in gaps)
        {
            evaluation.AddSkillGap(gap);
        }

        foreach (var m in matched)
        {
            evaluation.AddMatchedSkill(m);
        }

        evaluation.AddCitations(ragContext);

        var (summary, reasoning) = await BuildNarrativeAsync(candidate, job, fitScore, matched, gaps, ragContext, cancellationToken);
        evaluation.SetNarrative(summary, reasoning);

        return evaluation;
    }

    private async Task<(string Summary, string Reasoning)> BuildNarrativeAsync(
        Candidate candidate,
        JobDescription job,
        FitScore fitScore,
        IReadOnlyList<string> matched,
        IReadOnlyList<SkillGap> gaps,
        IReadOnlyList<Citation> ragContext,
        CancellationToken ct)
    {
        if (_textGeneration.IsLive)
        {
            try
            {
                var prompt = BuildPrompt(candidate, job, fitScore, matched, gaps, ragContext);
                var response = await _textGeneration.CompleteAsync(SystemPrompt, prompt, ct);
                var narrative = AgentJson.TryDeserialize<NarrativeDto>(response);
                if (narrative is not null && !string.IsNullOrWhiteSpace(narrative.Summary))
                {
                    return (narrative.Summary, narrative.Reasoning ?? string.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Job matching narrative AI call failed; using deterministic fallback.");
            }
        }

        return FallbackNarrative(candidate, job, fitScore, matched, gaps);
    }

    internal static (string Summary, string Reasoning) FallbackNarrative(
        Candidate candidate,
        JobDescription job,
        FitScore fitScore,
        IReadOnlyList<string> matched,
        IReadOnlyList<SkillGap> gaps)
    {
        var recommendation = RecommendationPolicy.FromScore(fitScore);
        var summary =
            $"{candidate.FullName} is a {recommendation} match for {job.Title} with a fit score of {fitScore}. " +
            $"Matches {matched.Count} of {job.RequiredSkills.Count + job.PreferredSkills.Count} tracked skills.";

        var reasoning = new StringBuilder();
        reasoning.Append($"Matched skills: {(matched.Count > 0 ? string.Join(", ", matched) : "none")}. ");
        var requiredGaps = gaps.Where(g => g.Required).Select(g => g.SkillName).ToList();
        reasoning.Append(requiredGaps.Count > 0
            ? $"Missing required skills: {string.Join(", ", requiredGaps)}. "
            : "No required skills are missing. ");
        reasoning.Append($"Experience: {candidate.TotalYearsExperience:0.#} years vs required {job.MinYearsExperience:0.#}.");

        return (summary, reasoning.ToString());
    }

    private static string BuildPrompt(
        Candidate candidate,
        JobDescription job,
        FitScore fitScore,
        IReadOnlyList<string> matched,
        IReadOnlyList<SkillGap> gaps,
        IReadOnlyList<Citation> ragContext)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"JOB TITLE: {job.Title}");
        sb.AppendLine($"REQUIRED SKILLS: {string.Join(", ", job.RequiredSkills.Select(s => s.Name))}");
        sb.AppendLine($"MIN EXPERIENCE: {job.MinYearsExperience} years");
        sb.AppendLine();
        sb.AppendLine($"CANDIDATE: {candidate.FullName}");
        sb.AppendLine($"CANDIDATE SKILLS: {string.Join(", ", candidate.Skills.Select(s => s.Name))}");
        sb.AppendLine($"CANDIDATE EXPERIENCE: {candidate.TotalYearsExperience} years");
        sb.AppendLine();
        sb.AppendLine($"COMPUTED FIT SCORE: {fitScore}");
        sb.AppendLine($"MATCHED SKILLS: {string.Join(", ", matched)}");
        sb.AppendLine($"SKILL GAPS: {string.Join(", ", gaps.Select(g => g.SkillName))}");
        sb.AppendLine();
        sb.AppendLine("RETRIEVED CONTEXT (grounding):");
        foreach (var c in ragContext)
        {
            sb.AppendLine($"- [{c.SourceName}] {c.Snippet}");
        }

        return sb.ToString();
    }

    private record NarrativeDto(string Summary, string? Reasoning);
}
