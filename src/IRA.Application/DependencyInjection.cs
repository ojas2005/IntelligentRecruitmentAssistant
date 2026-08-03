using FluentValidation;
using IRA.Application.Abstractions.Orchestration;
using IRA.Application.Common;
using IRA.Application.Features.Audit;
using IRA.Application.Features.Candidates;
using IRA.Application.Features.Evaluation;
using IRA.Application.Features.JobDescriptions;
using IRA.Application.DTOs;
using IRA.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IRA.Application;

/// <summary>Wires up the Application layer: services, orchestrator, handlers and validators.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Workflow services
        services.AddScoped<ResumeProcessingService>();
        services.AddScoped<JobDescriptionService>();

        // Semantic Kernel orchestrator (AI agent coordination)
        services.AddScoped<IRecruitmentOrchestrator, RecruitmentOrchestrator>();

        // Command handlers
        services.AddScoped<ICommandHandler<CreateJobDescriptionCommand, JobDescriptionDto>, CreateJobDescriptionCommandHandler>();
        services.AddScoped<ICommandHandler<EvaluateCandidatesCommand, RecruitmentEvaluationResultDto>, EvaluateCandidatesCommandHandler>();

        // Query handlers
        services.AddScoped<IQueryHandler<ListJobDescriptionsQuery, IReadOnlyList<JobDescriptionDto>>, ListJobDescriptionsQueryHandler>();
        services.AddScoped<IQueryHandler<GetRankingQuery, CandidateRankingDto?>, GetRankingQueryHandler>();
        services.AddScoped<IQueryHandler<ListCandidatesQuery, IReadOnlyList<CandidateDto>>, ListCandidatesQueryHandler>();
        services.AddScoped<IQueryHandler<GetAuditTrailQuery, IReadOnlyList<AuditEntryDto>>, GetAuditTrailQueryHandler>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<CreateJobDescriptionValidatorMarker>();

        return services;
    }
}

/// <summary>Marker type used to locate the assembly for FluentValidation registration.</summary>
public sealed class CreateJobDescriptionValidatorMarker;
