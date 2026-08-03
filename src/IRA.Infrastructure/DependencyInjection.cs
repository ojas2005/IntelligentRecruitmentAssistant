using IRA.Application.Abstractions.AI;
using IRA.Application.Abstractions.Agents;
using IRA.Application.Abstractions.Audit;
using IRA.Application.Abstractions.Documents;
using IRA.Application.Abstractions.Persistence;
using IRA.Application.Abstractions.Search;
using IRA.Application.Abstractions.Storage;
using IRA.Infrastructure.Agents;
using IRA.Infrastructure.AI;
using IRA.Infrastructure.Audit;
using IRA.Infrastructure.Configuration;
using IRA.Infrastructure.Documents;
using IRA.Infrastructure.Persistence;
using IRA.Infrastructure.Search;
using IRA.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IRA.Infrastructure;

/// <summary>
/// Wires up the Infrastructure layer. Each Azure-backed port is registered with its live
/// implementation when the corresponding configuration is present, and with a deterministic
/// offline fallback otherwise — delivering the "fallback during AI service disruption" requirement
/// and keeping the whole solution runnable before Azure keys are added.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var azure = new AzureOptions();
        configuration.GetSection(AzureOptions.SectionName).Bind(azure);
        services.AddSingleton(azure);
        services.AddSingleton(azure.OpenAI);
        services.AddSingleton(azure.Search);
        services.AddSingleton(azure.DocumentIntelligence);
        services.AddSingleton(azure.BlobStorage);

        // ----- Persistence, audit, RAG retrieval (always available) -----
        services.AddSingleton<ITalentRepository, InMemoryTalentRepository>();
        services.AddSingleton<IAuditLogger, InMemoryAuditLogger>();
        services.AddScoped<IRagRetrievalService, RagRetrievalService>();

        // ----- Semantic Kernel text generation (live vs. fallback via IsLive flag) -----
        services.AddSingleton<ITextGenerationService, SemanticKernelTextGenerationService>();

        // ----- Embeddings -----
        if (azure.OpenAI.IsConfigured)
        {
            services.AddSingleton<IEmbeddingGenerator, AzureOpenAIEmbeddingGenerator>();
        }
        else
        {
            services.AddSingleton<IEmbeddingGenerator, DeterministicEmbeddingGenerator>();
        }

        // ----- Vector store -----
        if (azure.Search.IsConfigured)
        {
            services.AddSingleton<IVectorStore>(sp => new AzureAiSearchVectorStore(
                azure.Search,
                azure.OpenAI.EmbeddingDimensions,
                sp.GetRequiredService<ILogger<AzureAiSearchVectorStore>>()));
        }
        else
        {
            services.AddSingleton<IVectorStore, InMemoryVectorStore>();
        }

        // ----- Document extraction -----
        if (azure.DocumentIntelligence.IsConfigured)
        {
            services.AddSingleton<IDocumentExtractor, AzureDocumentIntelligenceExtractor>();
        }
        else
        {
            services.AddSingleton<IDocumentExtractor, PlainTextDocumentExtractor>();
        }

        // ----- Blob storage -----
        if (azure.BlobStorage.IsConfigured)
        {
            services.AddSingleton<IBlobStorage, AzureBlobStorage>();
        }
        else
        {
            services.AddSingleton<IBlobStorage, LocalFileBlobStorage>();
        }

        // ----- AI Agents (implementations) -----
        services.AddScoped<IResumeParserAgent, ResumeParserAgent>();
        services.AddScoped<IJobMatchingAgent, JobMatchingAgent>();
        services.AddScoped<IInterviewAgent, InterviewAgent>();
        services.AddScoped<IReviewerAgent, ReviewerAgent>();
        services.AddScoped<IRankingAgent, RankingAgent>();

        return services;
    }
}
