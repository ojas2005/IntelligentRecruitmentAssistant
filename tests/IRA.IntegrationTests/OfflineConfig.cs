namespace IRA.IntegrationTests;

/// <summary>
/// Configuration overrides that force every Azure integration back to its offline fallback,
/// so integration tests stay deterministic even when a developer has real Azure keys in a
/// local secrets overlay (appsettings.Development.Local.json).
/// </summary>
public static class OfflineConfig
{
    public static readonly Dictionary<string, string?> Overrides = new()
    {
        ["Azure:OpenAI:Endpoint"] = "",
        ["Azure:OpenAI:ApiKey"] = "",
        ["Azure:Search:Endpoint"] = "",
        ["Azure:Search:ApiKey"] = "",
        ["Azure:DocumentIntelligence:Endpoint"] = "",
        ["Azure:DocumentIntelligence:ApiKey"] = "",
        ["Azure:BlobStorage:ConnectionString"] = "",
        ["Azure:CosmosDb:ConnectionString"] = "",
        ["Azure:CosmosDb:AccountEndpoint"] = "",
        ["Azure:CosmosDb:AccountKey"] = "",
    };
}
