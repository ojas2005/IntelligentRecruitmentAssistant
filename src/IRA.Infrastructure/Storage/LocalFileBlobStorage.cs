using IRA.Application.Abstractions.Storage;
using IRA.Infrastructure.Configuration;

namespace IRA.Infrastructure.Storage;

/// <summary>
/// Fallback blob storage that persists files to the local filesystem when
/// Azure Blob Storage is not configured.
/// </summary>
public class LocalFileBlobStorage : IBlobStorage
{
    private readonly string _root;

    public LocalFileBlobStorage(BlobStorageOptions options)
    {
        _root = Path.IsPathRooted(options.LocalPath)
            ? options.LocalPath
            : Path.Combine(AppContext.BaseDirectory, options.LocalPath);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string container, CancellationToken cancellationToken = default)
    {
        var dir = Path.Combine(_root, container);
        Directory.CreateDirectory(dir);
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(dir, safeName);

        await using var file = File.Create(fullPath);
        content.Position = 0;
        await content.CopyToAsync(file, cancellationToken);

        return Path.Combine(container, safeName);
    }

    public Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_root, path);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }
}
