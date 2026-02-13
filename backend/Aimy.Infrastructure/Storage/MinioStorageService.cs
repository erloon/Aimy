using Aimy.Core.Application.Interfaces;
using Minio;
using Minio.DataModel.Args;

namespace Aimy.Infrastructure.Storage;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minio;

    public MinioStorageService(IMinioClient minio)
    {
        _minio = minio;
    }

    public async Task<string> UploadAsync(
        Guid userId,
        string fileName,
        Stream fileStream,
        string? contentType,
        CancellationToken ct)
    {
        var bucketName = userId.ToString();
        
        // Ensure bucket exists for user
        await EnsureBucketExistsAsync(bucketName, ct);
        
        // Generate unique object name
        var objectName = $"{Guid.NewGuid()}_{fileName}";
        
        // Upload file to MinIO
        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType ?? "application/octet-stream"), ct);
        
        // Return storage path: {bucketName}/{objectName}
        return $"{bucketName}/{objectName}";
    }

    private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken ct)
    {
        var exists = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucketName), ct);
        
        if (!exists)
        {
            await _minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucketName), ct);
        }
    }
}
