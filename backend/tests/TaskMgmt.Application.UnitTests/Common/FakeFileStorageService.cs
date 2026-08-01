using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.UnitTests.Common;

internal class FakeFileStorageService : IFileStorageService
{
    public Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream());

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken) => Task.CompletedTask;
}
