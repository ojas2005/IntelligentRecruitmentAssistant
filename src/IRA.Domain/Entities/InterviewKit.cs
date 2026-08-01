using IRA.Domain.Common;

namespace IRA.Domain.Entities;

/// <summary>
/// The full set of interview questions generated for a candidate/role pairing
/// by the Interview Agent.
/// </summary>
public class InterviewKit : Entity
{
    private readonly List<InterviewQuestion> _questions = new();

    public Guid CandidateId { get; private set; }
    public Guid JobDescriptionId { get; private set; }
    public IReadOnlyCollection<InterviewQuestion> Questions => _questions;

    public InterviewKit(Guid candidateId, Guid jobDescriptionId)
    {
        CandidateId = candidateId;
        JobDescriptionId = jobDescriptionId;
    }

    private InterviewKit() { }

    public void Add(InterviewQuestion question) => _questions.Add(question);

    public void AddRange(IEnumerable<InterviewQuestion> questions) => _questions.AddRange(questions);
}
