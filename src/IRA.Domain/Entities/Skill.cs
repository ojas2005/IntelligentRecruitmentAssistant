using IRA.Domain.Enums;

namespace IRA.Domain.Entities;

/// <summary>
/// A single skill extracted from a resume or required by a job description.
/// </summary>
public class Skill
{
    public string Name { get; }
    public SkillCategory Category { get; }
    public int? YearsOfExperience { get; }

    public Skill(string name, SkillCategory category = SkillCategory.Technical, int? yearsOfExperience = null)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Skill name is required.", nameof(name))
            : name.Trim();
        Category = category;
        YearsOfExperience = yearsOfExperience;
    }

    /// <summary>Normalised key used for case-insensitive skill comparison.</summary>
    public string NormalizedName => Name.ToLowerInvariant();
}
