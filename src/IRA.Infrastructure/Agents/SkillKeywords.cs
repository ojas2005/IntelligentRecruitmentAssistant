namespace IRA.Infrastructure.Agents;

/// <summary>
/// A curated dictionary of well-known skills used by the deterministic fallback
/// resume parser to recognise skills in raw text when the LLM is unavailable.
/// </summary>
internal static class SkillKeywords
{
    public static readonly IReadOnlyList<string> Known = new[]
    {
        "C#", ".NET", "ASP.NET", "ASP.NET Core", "Azure", "Azure OpenAI", "Semantic Kernel",
        "JavaScript", "TypeScript", "React", "Angular", "Vue", "Node.js", "Python", "Java",
        "Go", "Rust", "C++", "SQL", "T-SQL", "PostgreSQL", "MySQL", "MongoDB", "Cosmos DB",
        "Redis", "Docker", "Kubernetes", "Terraform", "CI/CD", "DevOps", "Git", "REST",
        "GraphQL", "gRPC", "Microservices", "Entity Framework", "Blazor", "HTML", "CSS",
        "Machine Learning", "Deep Learning", "NLP", "TensorFlow", "PyTorch", "Pandas",
        "Kafka", "RabbitMQ", "Azure DevOps", "AWS", "GCP", "Linux", "Agile", "Scrum",
        "Unit Testing", "xUnit", "Selenium", "Power BI", "Data Engineering", "RAG"
    };
}
