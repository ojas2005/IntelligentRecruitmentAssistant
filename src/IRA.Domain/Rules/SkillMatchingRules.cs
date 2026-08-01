using IRA.Domain.Entities;
using IRA.Domain.ValueObjects;

namespace IRA.Domain.Rules;

/// <summary>
/// Deterministic skill-overlap scoring used as the grounding signal for candidate
/// fit and as a fallback when the AI service is unavailable.
/// </summary>
public static class SkillMatchingRules
{
    /// <summary>
    /// Computes a 0..100 fit score from the overlap between candidate skills and the
    /// job's required/preferred skills, plus an experience adequacy factor.
    /// Required skills are weighted more heavily than preferred skills.
    /// </summary>
    public static FitScore ComputeFitScore(Candidate candidate, JobDescription job)
    {
        var candidateSkills = candidate.Skills.Select(s => s.NormalizedName).ToHashSet();

        double requiredWeight = 0.7, preferredWeight = 0.2, experienceWeight = 0.1;

        double requiredScore = Ratio(job.RequiredSkills, candidateSkills);
        double preferredScore = job.PreferredSkills.Count == 0
            ? 1.0
            : Ratio(job.PreferredSkills, candidateSkills);

        double experienceScore = job.MinYearsExperience <= 0
            ? 1.0
            : Math.Clamp(candidate.TotalYearsExperience / job.MinYearsExperience, 0, 1);

        double total = (requiredScore * requiredWeight)
                       + (preferredScore * preferredWeight)
                       + (experienceScore * experienceWeight);

        return FitScore.FromFraction(total);
    }

    /// <summary>Identifies required/preferred skills the candidate is missing.</summary>
    public static IReadOnlyList<SkillGap> IdentifySkillGaps(Candidate candidate, JobDescription job)
    {
        var candidateSkills = candidate.Skills.Select(s => s.NormalizedName).ToHashSet();
        var gaps = new List<SkillGap>();

        foreach (var required in job.RequiredSkills)
        {
            if (!candidateSkills.Contains(required.NormalizedName))
            {
                gaps.Add(SkillGap.Missing(required.Name, required: true));
            }
        }

        foreach (var preferred in job.PreferredSkills)
        {
            if (!candidateSkills.Contains(preferred.NormalizedName))
            {
                gaps.Add(SkillGap.Missing(preferred.Name, required: false));
            }
        }

        return gaps;
    }

    /// <summary>Returns the candidate skills that satisfy the job's required/preferred skills.</summary>
    public static IReadOnlyList<string> MatchedSkills(Candidate candidate, JobDescription job)
    {
        var candidateSkills = candidate.Skills.Select(s => s.NormalizedName).ToHashSet();
        return job.RequiredSkills.Concat(job.PreferredSkills)
            .Where(js => candidateSkills.Contains(js.NormalizedName))
            .Select(js => js.Name)
            .Distinct()
            .ToList();
    }

    private static double Ratio(IReadOnlyCollection<Skill> jobSkills, HashSet<string> candidateSkills)
    {
        if (jobSkills.Count == 0)
        {
            return 1.0;
        }

        int matched = jobSkills.Count(js => candidateSkills.Contains(js.NormalizedName));
        return (double)matched / jobSkills.Count;
    }
}
