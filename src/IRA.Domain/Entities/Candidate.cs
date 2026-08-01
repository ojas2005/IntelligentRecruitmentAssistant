using IRA.Domain.Common;

namespace IRA.Domain.Entities;

/// <summary>
/// A person being evaluated for one or more roles. Aggregate root for the
/// structured profile extracted from their resume.
/// </summary>
public class Candidate : Entity
{
    private readonly List<Skill> _skills = new();
    private readonly List<Experience> _experiences = new();
    private readonly List<Education> _education = new();
    private readonly List<Certification> _certifications = new();

    public string FullName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Location { get; private set; }
    public double TotalYearsExperience { get; private set; }

    public IReadOnlyCollection<Skill> Skills => _skills;
    public IReadOnlyCollection<Experience> Experiences => _experiences;
    public IReadOnlyCollection<Education> Education => _education;
    public IReadOnlyCollection<Certification> Certifications => _certifications;

    public Candidate(string fullName, string? email = null, string? phone = null, string? location = null)
    {
        FullName = string.IsNullOrWhiteSpace(fullName)
            ? throw new DomainException("Candidate must have a name.")
            : fullName.Trim();
        Email = email;
        Phone = phone;
        Location = location;
    }

    // EF/serialization friendly ctor.
    private Candidate() => FullName = string.Empty;

    public void AddSkill(Skill skill)
    {
        if (_skills.Any(s => s.NormalizedName == skill.NormalizedName))
        {
            return; // de-duplicate skills (business rule)
        }

        _skills.Add(skill);
    }

    public void AddExperience(Experience experience)
    {
        _experiences.Add(experience);
        TotalYearsExperience = _experiences.Sum(e => e.Years);
    }

    /// <summary>
    /// Sets the aggregate years of experience directly. Used when a parser supplies a total
    /// without itemised experience entries. Ignored if itemised experiences already sum higher.
    /// </summary>
    public void SetTotalYearsExperience(double years)
    {
        if (years > TotalYearsExperience)
        {
            TotalYearsExperience = years;
        }
    }

    public void AddEducation(Education education) => _education.Add(education);

    public void AddCertification(Certification certification) => _certifications.Add(certification);

    public void UpdateContact(string? email, string? phone, string? location)
    {
        Email = email;
        Phone = phone;
        Location = location;
    }
}
