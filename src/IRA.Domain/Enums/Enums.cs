namespace IRA.Domain.Enums;

/// <summary>Classification of an uploaded recruitment document.</summary>
public enum DocumentCategory
{
    CandidateResume,
    JobDescription,
    CompetencyFramework,
    HiringPolicy,
    InterviewGuideline,
    SkillMatrix
}

/// <summary>Lifecycle status of a resume as it moves through the pipeline.</summary>
public enum ResumeStatus
{
    Uploaded,
    Extracted,
    Parsed,
    Embedded,
    Evaluated,
    Failed
}

/// <summary>Category a skill belongs to.</summary>
public enum SkillCategory
{
    Technical,
    Soft,
    Domain,
    Certification,
    Tool,
    Language
}

/// <summary>Type of an AI-generated interview question.</summary>
public enum QuestionType
{
    Technical,
    Behavioral,
    Situational
}

/// <summary>Relative difficulty of an interview question.</summary>
public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard
}

/// <summary>Final hiring recommendation for a candidate against a role.</summary>
public enum RecommendationLevel
{
    StrongReject,
    Reject,
    Consider,
    Shortlist,
    StrongShortlist
}

/// <summary>The specialised AI agents coordinated by Semantic Kernel.</summary>
public enum AgentRole
{
    ResumeParser,
    JobMatching,
    Interview,
    Reviewer,
    Ranking
}
