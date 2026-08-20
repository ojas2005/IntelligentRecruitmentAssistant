using IRA.Api.Auth;
using IRA.Api.Configuration;
using IRA.Api.Services;
using IRA.Application;
using IRA.Infrastructure;
using IRA.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ----- Local secrets overlay (gitignored): real Azure keys live here, never in source control -----
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.Local.json",
    optional: true,
    reloadOnChange: true);

// ----- Azure Key Vault (secret management): pull secrets when a vault URI is configured -----
var keyVaultUri = builder.Configuration[$"{AzureOptions.SectionName}:KeyVault:Uri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new Azure.Identity.DefaultAzureCredential());
}

// ----- Application + Infrastructure (clean architecture composition root) -----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ----- Application Insights (monitoring & observability) -----
var appInsights = builder.Configuration[$"{AzureOptions.SectionName}:ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(appInsights))
{
    builder.Services.AddApplicationInsightsTelemetry(o => o.ConnectionString = appInsights);
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ----- Authentication & Authorization -----
// Self-issued JWT with a username/password login endpoint gives the API real token auth
// out of the box. Signing key comes from config (Key Vault in prod); a development default
// keeps the API runnable locally with no configuration.
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (!jwtOptions.IsConfigured)
{
    jwtOptions.SigningKey = "dev-only-signing-key-change-me-in-production-0123456789";
}

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<JwtTokenService>();

// User accounts persist in Cosmos DB when configured (durable across restarts), else in-memory.
var cosmosOptions = builder.Configuration.GetSection($"{AzureOptions.SectionName}:CosmosDb").Get<CosmosDbOptions>() ?? new CosmosDbOptions();
if (cosmosOptions.IsConfigured)
{
    builder.Services.AddSingleton<IUserStore, CosmosUserStore>();
}
else
{
    builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new JwtTokenService(jwtOptions).ValidationParameters;
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(RecruitmentPolicies.Recruiters, p => p.RequireRole(RecruitmentRoles.RecruiterRoles))
    .AddPolicy(RecruitmentPolicies.Administrators, p => p.RequireRole(RecruitmentRoles.Administrator))
    .AddPolicy(RecruitmentPolicies.CandidatePortal, p => p.RequireRole(
        RecruitmentRoles.Candidate, RecruitmentRoles.Recruiter, RecruitmentRoles.HiringManager, RecruitmentRoles.Administrator))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Intelligent Recruitment Assistant API",
        Version = "v1",
        Description = "Backend service layer: resume processing, candidate evaluation, AI orchestration, ranking and audit."
    });

    // Allow pasting a bearer token (from /api/auth/login) into Swagger UI.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT returned by /api/auth/login."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

app.Run();

/// <summary>Exposed so the integration test project can spin up the API via WebApplicationFactory.</summary>
public partial class Program;
