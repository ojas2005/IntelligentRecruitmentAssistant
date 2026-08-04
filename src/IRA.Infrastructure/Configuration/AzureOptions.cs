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
    public KeyVaultOptions KeyVault { get; set; } = new();
    public ApplicationInsightsOptions ApplicationInsights { get; set; } = new();
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
