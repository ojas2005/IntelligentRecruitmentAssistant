using IRA.Domain.Enums;

namespace IRA.Domain.Entities;

/// <summary>An AI-generated interview question with its evaluation guidance.</summary>
public class InterviewQuestion
{
    public string Text { get; init; } = string.Empty;
    public QuestionType Type { get; init; }
    public DifficultyLevel Difficulty { get; init; }
    public string? EvaluationCriteria { get; init; }
    public string? TargetSkill { get; init; }
}
