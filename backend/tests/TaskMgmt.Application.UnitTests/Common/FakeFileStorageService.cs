using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.UnitTests.Common;

internal class FakeFileStorageService : IFileStorageService
{
    public List<string> UploadedKeys { get; } = [];
    public List<string> DeletedKeys { get; } = [];

    public Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken)
    {
        UploadedKeys.Add(storageKey);
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream());

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        DeletedKeys.Add(storageKey);
        return Task.CompletedTask;
    }
}
