using IRA.Domain.Common;

namespace IRA.Domain.Entities;

/// <summary>
/// A role definition that candidates are matched against.
/// </summary>
public class JobDescription : Entity
{
    private readonly List<Skill> _requiredSkills = new();
    private readonly List<Skill> _preferredSkills = new();

    public string Title { get; private set; }
    public string? Department { get; private set; }
    public string RawText { get; private set; }
    public double MinYearsExperience { get; private set; }
    public string BlobPath { get; private set; } = string.Empty;

    public IReadOnlyCollection<Skill> RequiredSkills => _requiredSkills;
    public IReadOnlyCollection<Skill> PreferredSkills => _preferredSkills;

    public JobDescription(string title, string rawText, string? department = null, double minYearsExperience = 0)
    {
        Title = string.IsNullOrWhiteSpace(title)
            ? throw new DomainException("Job description must have a title.")
            : title.Trim();
        RawText = rawText ?? string.Empty;
        Department = department;
        MinYearsExperience = minYearsExperience < 0 ? 0 : minYearsExperience;
    }

    private JobDescription()
    {
        Title = string.Empty;
        RawText = string.Empty;
    }

    public void AddRequiredSkill(Skill skill)
    {
        if (_requiredSkills.All(s => s.NormalizedName != skill.NormalizedName))
        {
            _requiredSkills.Add(skill);
        }
    }

    public void AddPreferredSkill(Skill skill)
    {
        if (_preferredSkills.All(s => s.NormalizedName != skill.NormalizedName))
        {
            _preferredSkills.Add(skill);
        }
    }

    public void SetBlobPath(string path) => BlobPath = path;
}
