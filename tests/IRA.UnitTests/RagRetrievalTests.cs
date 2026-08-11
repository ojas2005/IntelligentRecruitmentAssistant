using IRA.Application.Abstractions.AI;
using IRA.Application.Abstractions.Search;
using IRA.Domain.Enums;
using Xunit;

namespace IRA.UnitTests;

/// <summary>RAG Testing — embedding + vector search returns the most relevant grounding document.</summary>
public class RagRetrievalTests
{
    [Fact]
    public async Task Retrieval_returns_the_most_relevant_document_as_a_citation()
    {
        var provider = TestFactory.CreateProvider();
        var embeddings = provider.GetRequiredService<IEmbeddingGenerator>();
        var store = provider.GetRequiredService<IVectorStore>();
        var rag = provider.GetRequiredService<IRagRetrievalService>();

        await store.UpsertAsync(new VectorRecord
        {
            Id = "jd-1",
            Content = "Senior .NET developer role requiring C#, ASP.NET Core and Azure OpenAI experience.",
            SourceName = "Senior .NET JD",
            Category = DocumentCategory.JobDescription,
            Embedding = await embeddings.GenerateAsync("Senior .NET developer C# ASP.NET Core Azure OpenAI")
        });
        await store.UpsertAsync(new VectorRecord
        {
            Id = "jd-2",
            Content = "Marketing manager role focused on brand campaigns and social media.",
            SourceName = "Marketing JD",
            Category = DocumentCategory.JobDescription,
            Embedding = await embeddings.GenerateAsync("Marketing manager brand campaigns social media")
        });

        var citations = await rag.RetrieveAsync("Looking for a .NET engineer skilled in C# and Azure", topK: 1);

        Assert.Single(citations);
        Assert.Equal("Senior .NET JD", citations[0].SourceName);
        Assert.True(citations[0].Score > 0);
    }

    [Fact]
    public async Task Retrieval_can_filter_by_document_category()
    {
        var provider = TestFactory.CreateProvider();
        var embeddings = provider.GetRequiredService<IEmbeddingGenerator>();
        var store = provider.GetRequiredService<IVectorStore>();

        await store.UpsertAsync(new VectorRecord
        {
            Id = "r1", Content = "resume text C#", SourceName = "Resume A",
            Category = DocumentCategory.CandidateResume,
            Embedding = await embeddings.GenerateAsync("resume C# developer")
        });

        var results = await store.SearchAsync(
            await embeddings.GenerateAsync("C# developer"),
            topK: 5,
            categoryFilter: DocumentCategory.JobDescription);

        Assert.Empty(results); // no JD records exist
    }
}
