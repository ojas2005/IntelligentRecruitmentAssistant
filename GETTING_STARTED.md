# Getting Started — Intelligent Recruitment Assistant

Build, run and configuration guide for the .NET 9 solution. For the product overview and
architecture narrative, see [README.md](README.md).

> The solution runs **out of the box with no Azure keys** — every Azure integration has a
> deterministic offline fallback (this also satisfies the *"fallback during AI service
> disruption"* requirement). Add your keys in `appsettings.json` to switch on the live services.

---

## Flow of Execution

The Core Processing pipeline runs exactly as specified in the case study:

```
Extract → Analyze → Match → Generate Questions → Rank
```

1. **Extract** – `ResumeParserAgent` extracts a structured profile (skills, experience,
   certifications, education) from text pulled by **Azure Document Intelligence**.
2. **Analyze / Match** – `JobMatchingAgent` compares the candidate to the job description,
   grounded by **RAG** retrieval over the vector store, computing a fit score + skill gaps.
3. **Reviewer validation** – `ReviewerAgent` validates each evaluation for consistency/fairness.
4. **Generate Questions** – `InterviewAgent` produces technical, behavioural and situational
   questions for the shortlist.
5. **Rank** – `RankingAgent` aggregates validated evaluations into a prioritised shortlist,
   re-validated by the `ReviewerAgent` before presentation.

The **Semantic Kernel Orchestrator** ([`RecruitmentOrchestrator`](src/IRA.Application/Services/RecruitmentOrchestrator.cs))
coordinates these agents and audit-logs every stage.

---

## Projects (Clean Architecture)

| Project | Layer | Responsibility |
|---|---|---|
| `IRA.Domain` | Domain | Entities, value objects, enums, business & validation rules. **No external dependencies.** |
| `IRA.Application` | Application | Use cases, DTOs, validators, CQRS handlers, agent/port interfaces, orchestrator. |
| `IRA.Infrastructure` | Infrastructure | Azure OpenAI, AI Search, Document Intelligence, Blob, Semantic Kernel, agents, vector store, talent repo, audit — each with an offline fallback. |
| `IRA.Api` | API | ASP.NET Core Web API — Auth, Resume, Candidate, JobDescription, Matching, Interview, Ranking, Audit. Secured with Microsoft Entra ID. |
| `IRA.Web` | Presentation | ASP.NET Core MVC — Dashboard, Resume Upload, JD Management, Candidate, Ranking, Analytics. |
| `IRA.UnitTests` / `IRA.IntegrationTests` | Tests | xUnit. |

Dependencies point inward: `Api`/`Web` → `Application` → `Domain`; `Infrastructure` → `Application`/`Domain`.

---

## Prerequisites

- .NET SDK 9 or 10 (the projects target `net9.0`).

## Build & run

```bash
dotnet build IntelligentRecruitmentAssistant.slnx
```

Run the **API** (backend) and the **MVC** frontend in two terminals:

```bash
dotnet run --project src/IRA.Api      # http://localhost:5180  (Swagger at /swagger)
```

```bash
dotnet run --project src/IRA.Web      # http://localhost:5280
```

### Sign in (JWT)

Without Microsoft Entra ID configured, the app uses a **self-issued JWT** scheme with a
username/password login. The MVC frontend signs you in and forwards the token to the API.
Two role-based portals are served from the same app:

| Portal | Who | What they can do |
|---|---|---|
| **Recruiter portal** | `Recruiter`, `HiringManager`, `RecruitmentAdministrator` | Dashboard, bulk resume upload, job descriptions, candidates, ranking/evaluation, analytics (admin) |
| **Candidate portal** ("user") | `Candidate` | Upload own resume, view own parsed profile, browse open roles |

Seeded demo accounts (password `Passw0rd!` for all):

| Username | Role |
|---|---|
| `recruiter` | Recruiter |
| `manager` | Hiring Manager |
| `admin` | Administrator (sees Analytics) |
| `candidate` | Candidate |

Candidates can also self-register from the login page. Get a token directly from the API with:

```bash
curl -s -X POST http://localhost:5180/api/auth/login -H "Content-Type: application/json" \
  -d '{"username":"recruiter","password":"Passw0rd!"}'
```

Pass it as `Authorization: Bearer <token>` on API calls (or paste it into Swagger's **Authorize** dialog).

### Quick API smoke test

```bash
# create a job description
curl -X POST http://localhost:5180/api/jobdescription -H "Content-Type: application/json" \
  -d '{"title":"Senior Backend Engineer","rawText":"Need C#, ASP.NET Core, Azure","minYearsExperience":5,"requiredSkills":["C#","ASP.NET Core","Azure"],"preferredSkills":["Docker"]}'

# upload a resume (any .txt/.md/.pdf/.docx)
curl -X POST http://localhost:5180/api/resume/upload -F "file=@resume.txt"

# run the orchestrated evaluation (use the id returned above)
curl -X POST http://localhost:5180/api/matching/evaluate -H "Content-Type: application/json" \
  -d '{"jobDescriptionId":"<JOB_ID>","interviewShortlistSize":5}'
```

---

## Adding your Azure keys

Fill in the blank sections of [`src/IRA.Api/appsettings.json`](src/IRA.Api/appsettings.json)
(and `AzureAd` in [`src/IRA.Web/appsettings.json`](src/IRA.Web/appsettings.json)). Any section
left blank transparently uses the offline fallback.

```jsonc
"Azure": {
  "OpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "ApiKey": "<your-key>",
    "ChatDeployment": "gpt-4o",
    "EmbeddingDeployment": "text-embedding-3-small",
    "EmbeddingDimensions": 1536
  },
  "Search":               { "Endpoint": "https://<your-search>.search.windows.net", "ApiKey": "<key>", "IndexName": "recruitment-index" },
  "DocumentIntelligence": { "Endpoint": "https://<your-di>.cognitiveservices.azure.com/", "ApiKey": "<key>" },
  "BlobStorage":          { "ConnectionString": "<storage-connection-string>" },
  "KeyVault":             { "Uri": "https://<your-vault>.vault.azure.net/" },
  "ApplicationInsights":  { "ConnectionString": "<app-insights-connection-string>" }
},
"AzureAd": { "TenantId": "<tenant>", "ClientId": "<client-id>", "Audience": "<audience>" }
```

**Never commit real keys.** For production, set `Azure:KeyVault:Uri` and store secrets in
Azure Key Vault (loaded automatically via `DefaultAzureCredential`), or use user-secrets:

```bash
dotnet user-secrets set "Azure:OpenAI:ApiKey" "<your-key>" --project src/IRA.Api
```

Which implementation activates per section:

| Section | Live implementation | Offline fallback |
|---|---|---|
| `OpenAI` | Semantic Kernel + Azure OpenAI chat/embeddings | Deterministic agents + hash embeddings |
| `Search` | Azure AI Search vector store | In-memory cosine vector store |
| `DocumentIntelligence` | Azure Document Intelligence | Plain-text extractor |
| `BlobStorage` | Azure Blob Storage | Local filesystem |
| `AzureAd` | Microsoft Entra ID (JWT / OpenID Connect) | Self-issued JWT login (seeded demo accounts, candidate self-registration) |
| `Jwt` | — (signing key for the self-issued JWT; set in Key Vault for prod) | Development signing key |

---

## Tests

```bash
dotnet test IntelligentRecruitmentAssistant.slnx
```

Covering every test type named in the case study:

| Case-study test | Location |
|---|---|
| Unit Testing | `tests/IRA.UnitTests/DomainRulesTests.cs` |
| Resume Parsing Testing | `tests/IRA.UnitTests/ResumeParsingTests.cs` |
| Candidate Matching Testing | `tests/IRA.UnitTests/CandidateMatchingTests.cs` |
| RAG Testing | `tests/IRA.UnitTests/RagRetrievalTests.cs` |
| AI Agent Workflow Testing | `tests/IRA.UnitTests/AgentWorkflowTests.cs` |
| Interview Question Generation Testing | `tests/IRA.UnitTests/InterviewQuestionGenerationTests.cs` |
| Authorization Testing | `tests/IRA.IntegrationTests/AuthorizationTests.cs` (401 / 403 / 200) |
| JWT Authentication Testing | `tests/IRA.IntegrationTests/JwtAuthenticationTests.cs` (login, bearer, role gates, candidate self-service) |
| Integration Testing | `tests/IRA.IntegrationTests/RecruitmentFlowIntegrationTests.cs` (end-to-end over HTTP) |

All 37 tests pass with no Azure configuration.
