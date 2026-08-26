using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.UnitTests.Common;

internal class FakeFileStorageService : IFileStorageService
{
    public List<string> UploadedKeys { get; } = [];
    public List<string> DeletedKeys { get; } = [];
    public List<string> CallOrder { get; } = [];

    public Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken)
    {
        UploadedKeys.Add(storageKey);
        CallOrder.Add($"upload:{storageKey}");
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream());

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        DeletedKeys.Add(storageKey);
        CallOrder.Add($"delete:{storageKey}");
        return Task.CompletedTask;
    }
}
