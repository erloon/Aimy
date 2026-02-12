---
name: minio-storage
description: MinIO object storage integration for .NET applications. Use when working with file storage, S3-compatible storage, bucket operations, object uploads/downloads, presigned URLs, or configuring MinIO client in ASP.NET Core. Handles file persistence, media storage, document management, and any S3-compatible storage operations.
---

# MinIO Storage

## Overview

MinIO is an S3-compatible object storage server used in this project for file storage. This skill covers .NET SDK integration patterns for ASP.NET Core applications.

## Setup

### Install Package

```bash
dotnet add package Minio
```

### DI Registration (Program.cs)

```csharp
using Minio;

var builder = WebApplication.CreateBuilder(args);

// Option 1: Simple registration with credentials
builder.Services.AddMinio("accessKey", "secretKey");

// Option 2: Full configuration with custom endpoint
builder.Services.AddMinio(configureClient => configureClient
    .WithEndpoint("localhost:9000")  // or your MinIO server
    .WithCredentials("accessKey", "secretKey")
    .WithSSL(false)  // disable for local dev
    .Build());
```

### Configuration (appsettings.json)

```json
{
  "MinIO": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "Secure": false
  }
}
```

## Usage Patterns

### Inject IMinioClient

```csharp
public class FileService
{
    private readonly IMinioClient _minio;

    public FileService(IMinioClient minio) => _minio = minio;
}
```

### Create Bucket

```csharp
public async Task EnsureBucketExists(string bucketName)
{
    var exists = await _minio.BucketExistsAsync(
        new BucketExistsArgs().WithBucket(bucketName));
    
    if (!exists)
    {
        await _minio.MakeBucketAsync(
            new MakeBucketArgs().WithBucket(bucketName));
    }
}
```

### Upload File

```csharp
public async Task UploadFile(string bucket, string objectName, string filePath, string contentType)
{
    await _minio.PutObjectAsync(new PutObjectArgs()
        .WithBucket(bucket)
        .WithObject(objectName)
        .WithFileName(filePath)
        .WithContentType(contentType));
}
```

### Upload from Stream

```csharp
public async Task UploadStream(string bucket, string objectName, Stream data, long size, string contentType)
{
    await _minio.PutObjectAsync(new PutObjectArgs()
        .WithBucket(bucket)
        .WithObject(objectName)
        .WithStreamData(data)
        .WithObjectSize(size)
        .WithContentType(contentType));
}
```

### Download File

```csharp
public async Task<Stream> DownloadFile(string bucket, string objectName)
{
    var memoryStream = new MemoryStream();
    await _minio.GetObjectAsync(new GetObjectArgs()
        .WithBucket(bucket)
        .WithObject(objectName)
        .WithCallbackStream(stream => stream.CopyTo(memoryStream)));
    memoryStream.Position = 0;
    return memoryStream;
}
```

### Generate Presigned URL

```csharp
public async Task<string> GetPresignedUrl(string bucket, string objectName, int expirySeconds = 3600)
{
    return await _minio.PresignedGetObjectAsync(new PresignedGetObjectArgs()
        .WithBucket(bucket)
        .WithObject(objectName)
        .WithExpiry(expirySeconds));
}
```

### Delete Object

```csharp
public async Task DeleteObject(string bucket, string objectName)
{
    await _minio.RemoveObjectAsync(new RemoveObjectArgs()
        .WithBucket(bucket)
        .WithObject(objectName));
}
```

### List Objects

```csharp
public async Task<List<string>> ListObjects(string bucket, string prefix = "")
{
    var objects = new List<string>();
    await foreach (var item in _minio.ListObjectsEnumAsync(
        new ListObjectsArgs().WithBucket(bucket).WithPrefix(prefix)))
    {
        objects.Add(item.Key);
    }
    return objects;
}
```

## API Controller Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IMinioClient _minio;
    private const string BucketName = "uploads";

    public FilesController(IMinioClient minio) => _minio = minio;

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var objectName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        
        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(file.Length)
            .WithContentType(file.ContentType));

        return Ok(new { objectName });
    }

    [HttpGet("{objectName}/url")]
    public async Task<IActionResult> GetUrl(string objectName)
    {
        var url = await _minio.PresignedGetObjectAsync(
            new PresignedGetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectName)
                .WithExpiry(3600));
        
        return Ok(new { url });
    }
}
```

## Using IMinioClientFactory

For multiple configurations or per-request client creation:

```csharp
public class MultiTenantStorageService
{
    private readonly IMinioClientFactory _factory;

    public MultiTenantStorageService(IMinioClientFactory factory) => _factory = factory;

    public async Task UploadToTenant(string tenantId, string objectName, Stream data)
    {
        var client = _factory.CreateClient(); // Optional: pass configure action
        // Use client...
    }
}
```

## Common Args Classes

| Operation | Args Class | Key Methods |
|-----------|-----------|-------------|
| Bucket exists | `BucketExistsArgs` | `.WithBucket()` |
| Make bucket | `MakeBucketArgs` | `.WithBucket()`, `.WithLocation()` |
| Upload | `PutObjectArgs` | `.WithBucket()`, `.WithObject()`, `.WithFileName()` / `.WithStreamData()` |
| Download | `GetObjectArgs` | `.WithBucket()`, `.WithObject()`, `.WithCallbackStream()` |
| Delete | `RemoveObjectArgs` | `.WithBucket()`, `.WithObject()` |
| Presigned GET | `PresignedGetObjectArgs` | `.WithBucket()`, `.WithObject()`, `.WithExpiry()` |
| Presigned PUT | `PresignedPutObjectArgs` | `.WithBucket()`, `.WithObject()`, `.WithExpiry()` |
| List objects | `ListObjectsArgs` | `.WithBucket()`, `.WithPrefix()`, `.WithRecursive()` |
