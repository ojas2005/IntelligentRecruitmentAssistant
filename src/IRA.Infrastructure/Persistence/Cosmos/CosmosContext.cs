using IRA.Infrastructure.Configuration;
using Microsoft.Azure.Cosmos;

namespace IRA.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Owns the singleton <see cref="CosmosClient"/> and lazily provisions the database and
/// containers (idempotent "create if not exists"). Containers are camelCase-serialised so
/// partition-key paths such as <c>/jobDescriptionId</c> match the stored property names.
/// </summary>
public sealed class CosmosContext : IAsyncDisposable
{
    // Container ids and their partition-key paths.
    public const string Candidates = "candidates";
    public const string Resumes = "resumes";
    public const string Jobs = "jobDescriptions";
    public const string Evaluations = "evaluations";
    public const string InterviewKits = "interviewKits";
    public const string Rankings = "rankings";
    public const string Audit = "audit";
    public const string Users = "users";

    private static readonly (string Id, string PartitionKey)[] Definitions =
    {
        (Candidates, "/id"),
        (Resumes, "/id"),
        (Jobs, "/id"),
        (Evaluations, "/jobDescriptionId"),
        (InterviewKits, "/jobDescriptionId"),
        (Rankings, "/id"),
        (Audit, "/id"),
        (Users, "/id"),
    };

    private readonly CosmosClient _client;
    private readonly CosmosDbOptions _options;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private Database? _database;
    private bool _initialised;

    public CosmosContext(CosmosDbOptions options)
    {
        _options = options;
        var clientOptions = new CosmosClientOptions
        {
            ApplicationName = "IntelligentRecruitmentAssistant",
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        _client = !string.IsNullOrWhiteSpace(options.ConnectionString)
            ? new CosmosClient(options.ConnectionString, clientOptions)
            : new CosmosClient(options.AccountEndpoint, options.AccountKey, clientOptions);
    }

    /// <summary>Returns a ready-to-use container, provisioning the database/containers on first call.</summary>
    public async Task<Container> GetContainerAsync(string id, CancellationToken ct = default)
    {
        if (!_initialised)
        {
            await InitialiseAsync(ct);
        }

        return _database!.GetContainer(id);
    }

    private async Task InitialiseAsync(CancellationToken ct)
    {
        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialised)
            {
                return;
            }

            // Provisioned accounts: one shared throughput pool for the whole database, so all
            // containers together cost a single 400 RU/s (inside the free tier). Serverless
            // accounts provision nothing. Either way containers are created without their own RU/s.
            _database = _options.Serverless
                ? await _client.CreateDatabaseIfNotExistsAsync(_options.DatabaseName, cancellationToken: ct)
                : await _client.CreateDatabaseIfNotExistsAsync(_options.DatabaseName, _options.Throughput ?? 400, cancellationToken: ct);

            foreach (var (containerId, partitionKey) in Definitions)
            {
                var properties = new ContainerProperties(containerId, partitionKey);
                await _database.CreateContainerIfNotExistsAsync(properties, cancellationToken: ct);
            }

            _initialised = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
