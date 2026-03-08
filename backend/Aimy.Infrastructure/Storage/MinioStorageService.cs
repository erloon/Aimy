using Aimy.Core.Application.Interfaces;
using Aimy.Core.Application.Interfaces.Upload;
using Minio;
using Minio.DataModel.Args;

namespace Aimy.Infrastructure.Storage;

public class MinioStorageService(IMinioClient minio) : IStorageService
{
    public async Task<string> UploadAsync(
        Guid userId,
        string bucketName,
        string fileName,
        Stream fileStream,
        string? contentType,
        CancellationToken ct)
    {
        // Ensure bucket exists
        await EnsureBucketExistsAsync(bucketName, ct);
        
        // Generate hierarchical object name with userId prefix
        var objectName = StorageKeyFormat.KbItemKey(userId, fileName);
        
        // Upload file to MinIO
        await minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType ?? "application/octet-stream"), ct);
        
        // Return storage path: {bucketName}/{objectName}
        return $"{bucketName}/{objectName}";
    }

    public async Task<Stream> DownloadAsync(string storagePath, CancellationToken ct)
    {
        var (bucketName, objectName) = ParseStoragePath(storagePath);
        
        var memoryStream = new MemoryStream();
        await minio.GetObjectAsync(new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithCallbackStream(async (stream, _) => 
            {
                await stream.CopyToAsync(memoryStream, ct);
            }), ct);
        
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteAsync(string storagePath, CancellationToken ct)
    {
        var (bucketName, objectName) = ParseStoragePath(storagePath);
        
        await minio.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName), ct);
    }

    private static (string bucketName, string objectName) ParseStoragePath(string storagePath)
    {
        var separatorIndex = storagePath.IndexOf('/');
        if (separatorIndex < 0)
        {
            throw new ArgumentException($"Invalid storage path format: {storagePath}. Expected format: {{bucketName}}/{{objectName}}", nameof(storagePath));
        }
        
        var bucketName = storagePath.Substring(0, separatorIndex);
        var objectName = storagePath.Substring(separatorIndex + 1);
        
        return (bucketName, objectName);
    }

    private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken ct)
    {
        var exists = await minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucketName), ct);
        
        if (!exists)
        {
            await minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucketName), ct);
        }
    }
}
