namespace TaskMgmt.Application.Common.Models;

public class StorageSettings
{
    public const string SectionName = "Storage";

    public required string Endpoint { get; set; }
    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }
    public required string BucketName { get; set; }
    public bool UseSsl { get; set; } = false;
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
}
