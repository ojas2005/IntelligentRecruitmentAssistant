namespace IRA.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed Azure configuration bound from appsettings / Key Vault.
/// Leave endpoints/keys blank to run the solution against the built-in offline fallbacks.
/// </summary>
public class AzureOptions
{
    public const string SectionName = "Azure";

    public AzureOpenAIOptions OpenAI { get; set; } = new();
    public AzureSearchOptions Search { get; set; } = new();
    public DocumentIntelligenceOptions DocumentIntelligence { get; set; } = new();
    public BlobStorageOptions BlobStorage { get; set; } = new();
    public CosmosDbOptions CosmosDb { get; set; } = new();
    public KeyVaultOptions KeyVault { get; set; } = new();
    public ApplicationInsightsOptions ApplicationInsights { get; set; } = new();
}

public class CosmosDbOptions
{
    /// <summary>Full connection string (alternative to AccountEndpoint + AccountKey).</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>e.g. https://your-account.documents.azure.com:443/ — used with AccountKey.</summary>
    public string AccountEndpoint { get; set; } = string.Empty;

    /// <summary>Primary/secondary key. Prefer Key Vault / Managed Identity in production.</summary>
    public string AccountKey { get; set; } = string.Empty;

    /// <summary>Database created if it does not exist.</summary>
    public string DatabaseName { get; set; } = "RecruitmentDb";

    /// <summary>
    /// Set true only for a serverless Cosmos account (no throughput is provisioned). For a
    /// provisioned account (incl. the free tier) leave false so containers share one throughput pool.
    /// </summary>
    public bool Serverless { get; set; }

    /// <summary>
    /// Shared database throughput (RU/s) for provisioned accounts. All containers share this one
    /// pool, so the whole app costs a single 400 RU/s — fitting inside the 1000 RU/s free tier.
    /// Ignored when <see cref="Serverless"/> is true.
    /// </summary>
    public int? Throughput { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ConnectionString) ||
        (!string.IsNullOrWhiteSpace(AccountEndpoint) && !string.IsNullOrWhiteSpace(AccountKey));
}

public class AzureOpenAIOptions
{
    /// <summary>e.g. https://your-resource.openai.azure.com/ — leave blank for fallback.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>API key. Prefer Key Vault / Managed Identity in production.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Chat/completion deployment name (e.g. gpt-4o).</summary>
    public string ChatDeployment { get; set; } = "gpt-4o";

    /// <summary>Embedding deployment name (e.g. text-embedding-3-small).</summary>
    public string EmbeddingDeployment { get; set; } = "text-embedding-3-small";

    /// <summary>Dimensionality of the embedding model (1536 for text-embedding-3-small).</summary>
    public int EmbeddingDimensions { get; set; } = 1536;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey);
}

public class AzureSearchOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = "recruitment-index";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey);
}

public class DocumentIntelligenceOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey);
}

public class BlobStorageOptions
{
    /// <summary>Blob storage connection string — leave blank to store files on the local disk.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Local directory used by the fallback storage provider.</summary>
    public string LocalPath { get; set; } = "App_Data/blobs";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);
}

public class KeyVaultOptions
{
    /// <summary>e.g. https://your-vault.vault.azure.net/ — enables Key Vault-backed configuration.</summary>
    public string Uri { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Uri);
}

public class ApplicationInsightsOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);
}
