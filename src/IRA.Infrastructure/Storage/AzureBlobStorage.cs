using Azure.Storage.Blobs;
using IRA.Application.Abstractions.Storage;
using IRA.Infrastructure.Configuration;
using IRA.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;

namespace IRA.Infrastructure.Storage;

/// <summary>Live blob storage backed by Azure Blob Storage.</summary>
public class AzureBlobStorage : IBlobStorage
{
    private readonly BlobServiceClient _client;
    private readonly ILogger<AzureBlobStorage> _logger;

    public AzureBlobStorage(BlobStorageOptions options, ILogger<AzureBlobStorage> logger)
    {
        _client = new BlobServiceClient(options.ConnectionString);
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string container, CancellationToken cancellationToken = default)
    {
        var containerClient = _client.GetBlobContainerClient(container);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var blob = containerClient.GetBlobClient(blobName);

        await RetryExecutor.ExecuteAsync(async ct =>
        {
            content.Position = 0;
            await blob.UploadAsync(content, overwrite: true, ct);
            return true;
        }, _logger, cancellationToken: cancellationToken);

        return $"{container}/{blobName}";
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        var slash = path.IndexOf('/');
        var container = path[..slash];
        var blobName = path[(slash + 1)..];
        var blob = _client.GetBlobContainerClient(container).GetBlobClient(blobName);
        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }
}
