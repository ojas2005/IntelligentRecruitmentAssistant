namespace IRA.Domain.ValueObjects;

/// <summary>
/// Represents a single missing or under-developed skill discovered while comparing
/// a candidate against a job description.
/// </summary>
/// <param name="SkillName">The required skill.</param>
/// <param name="Required">Whether the JD lists this skill as mandatory.</param>
/// <param name="Severity">0..1 — how significant the gap is (1 = critical, missing mandatory skill).</param>
public readonly record struct SkillGap(string SkillName, bool Required, double Severity)
{
    public static SkillGap Missing(string skillName, bool required) =>
        new(skillName, required, required ? 1.0 : 0.5);
}
