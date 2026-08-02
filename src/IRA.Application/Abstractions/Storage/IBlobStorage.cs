namespace IRA.Application.Abstractions.Storage;

/// <summary>Stores raw resume and job-description files (Azure Blob Storage, with a local fallback).</summary>
public interface IBlobStorage
{
    Task<string> UploadAsync(Stream content, string fileName, string container, CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default);
}
