namespace TaskMgmt.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
