using IRA.Application.Abstractions.Audit;
using IRA.Domain.Entities;
using IRA.Infrastructure.Persistence.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace IRA.Infrastructure.Audit;

/// <summary>
/// Durable audit trail in Azure Cosmos DB. Every entry is also mirrored to the logging pipeline
/// (which flows to Application Insights when configured), preserving the existing observability.
/// </summary>
public sealed class CosmosAuditLogger : IAuditLogger
{
    private readonly CosmosContext _context;
    private readonly ILogger<CosmosAuditLogger> _logger;

    public CosmosAuditLogger(CosmosContext context, ILogger<CosmosAuditLogger> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AUDIT {Actor} {Action} {EntityType}#{EntityId} {Details}",
            entry.Actor, entry.Action, entry.EntityType, entry.EntityId, entry.Details);

        var container = await _context.GetContainerAsync(CosmosContext.Audit, cancellationToken);
        var doc = AuditDocument.From(entry);
        await container.UpsertItemAsync(doc, new PartitionKey(doc.Id), cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntry>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        var container = await _context.GetContainerAsync(CosmosContext.Audit, cancellationToken);
        var query = new QueryDefinition("SELECT * FROM c ORDER BY c.timestampUtc DESC OFFSET 0 LIMIT @count")
            .WithParameter("@count", count);

        var results = new List<AuditEntry>();
        using var iterator = container.GetItemQueryIterator<AuditDocument>(query);
        while (iterator.HasMoreResults)
        {
            foreach (var doc in await iterator.ReadNextAsync(cancellationToken))
            {
                results.Add(doc.ToDomain());
            }
        }

        return results;
    }
}
