using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Common.Models;

namespace TaskMgmt.Infrastructure.Storage;

// Dùng AWS S3 SDK để nói chuyện với MinIO (tương thích S3 API). ForcePathStyle bắt buộc
// với MinIO vì nó không hỗ trợ virtual-hosted-style bucket addressing như AWS S3 thật.
public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _client;
    private readonly StorageSettings _settings;
    private volatile bool _bucketEnsured;

    public S3FileStorageService(StorageSettings settings)
    {
        _settings = settings;
        _client = new AmazonS3Client(
            settings.AccessKey,
            settings.SecretKey,
            new AmazonS3Config
            {
                ServiceURL = settings.Endpoint,
                ForcePathStyle = true,
                UseHttp = !settings.UseSsl,
            });
    }

    public async Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        await _client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = storageKey,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false,
            },
            cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var response = await _client.GetObjectAsync(_settings.BucketName, storageKey, cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        await _client.DeleteObjectAsync(_settings.BucketName, storageKey, cancellationToken);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        if (_bucketEnsured)
        {
            return;
        }

        if (!await AmazonS3Util.DoesS3BucketExistV2Async(_client, _settings.BucketName))
        {
            try
            {
                await _client.PutBucketAsync(_settings.BucketName, cancellationToken);
            }
            catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou")
            {
                // Bucket đã được tạo bởi một request khác chạy song song lúc khởi động - bỏ qua.
            }
        }

        _bucketEnsured = true;
    }
}
