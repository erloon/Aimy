# Learnings - Add File Endpoints

## 2026-02-13: Task 1 - Define Core Interfaces & DTOs

### Patterns Observed
- DTOs use `{ get; set; }` properties with `required` keyword for mandatory fields
- Optional strings use `string?`
- All async methods include `CancellationToken ct` parameter
- Generic types work well for paged results: `PagedResult<T>`

### File Structure
- DTOs go in `backend/Aimy.Core/Application/DTOs/`
- Interfaces go in `backend/Aimy.Core/Application/Interfaces/`
- Must add `using Aimy.Core.Application.DTOs;` when referencing DTOs

### Build Dependency
- When adding interface methods to an existing interface that's already implemented, need to add stub implementations (throw NotImplementedException) to keep build passing
- Stub implementations are acceptable placeholders for later task implementation

### PagedResult<T> Pattern
```csharp
public class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; set; }
    public required int Page { get; set; }
    public required int PageSize { get; set; }
    public required int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

## 2026-02-13: Task 2 - Implement Infrastructure Adapters

### MinIO Storage Patterns
- Storage path format: `{bucketName}/{objectName}` (e.g., `user-guid-123/filename.pdf`)
- Download uses `GetObjectArgs` with `WithCallbackStream` to copy to MemoryStream
- Delete uses `RemoveObjectArgs` with bucket and object name
- Parse storage path by splitting on first `/` separator

### MinIO Download Implementation
```csharp
public async Task<Stream> DownloadAsync(string storagePath, CancellationToken ct)
{
    var (bucketName, objectName) = ParseStoragePath(storagePath);
    var memoryStream = new MemoryStream();
    await _minio.GetObjectAsync(new GetObjectArgs()
        .WithBucket(bucketName)
        .WithObject(objectName)
        .WithCallbackStream(async (stream, _) => 
        {
            await stream.CopyToAsync(memoryStream, ct);
        }), ct);
    memoryStream.Position = 0;
    return memoryStream;
}
```

### EF Core Pagination Pattern
```csharp
public async Task<PagedResult<Upload>> GetPagedAsync(Guid userId, int page, int pageSize, CancellationToken ct)
{
    var query = _context.Uploads.Where(u => u.UserId == userId);
    var totalCount = await query.CountAsync(ct);
    var items = await query
        .OrderByDescending(u => u.DateUploaded)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);
    
    return new PagedResult<Upload>
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount
    };
}
```

### Repository Delete/Update Patterns
- Delete: Use `FindAsync([id], ct)` then `Remove()` and `SaveChangesAsync()`
- Update: Use `Update()` then `SaveChangesAsync()`
- Delete silently ignores non-existent entities (no exception thrown)

## 2026-02-13: Task 3 - Service Logic with TDD

### Service Authorization/Ownership Pattern
- Resolve user first with `GetCurrentUserId()` and throw `UnauthorizedAccessException("User is not authenticated")` when null
- For id-based operations, load upload via `GetByIdAsync(id, ct)` and throw `KeyNotFoundException("File not found")` when missing
- Enforce ownership with `upload.UserId != userId.Value` and throw `UnauthorizedAccessException("User does not have access to this file")`

### Mapping and Pagination Pattern
- Reuse a single mapper from `Upload` to `UploadFileResponse` for list/update responses
- For `ListAsync`, map repository `PagedResult<Upload>` into `PagedResult<UploadFileResponse>` and preserve `Page`, `PageSize`, `TotalCount`

### Delete Consistency Pattern
- Delete from storage first, then delete repository record
- This prevents DB deletion when object storage deletion fails

### TDD Test Pattern Used
- Add success + unauthenticated + not-found + ownership-failure tests per new service method
- Keep Moq verification explicit (`Times.Once` / `Times.Never`) for repo/storage interactions

## 2026-02-13: Task 4 - Expose API Endpoints

### Endpoint Organization Pattern
- Separate route groups for singular `/upload` vs plural `/uploads` operations
- Use `WithTags()` to group related endpoints in Swagger/OpenAPI docs
- All file management endpoints use `RequireAuthorization()`

### Pagination Endpoint Pattern
- Default values: `page = 1`, `pageSize = 10`
- Validation: page >= 1, pageSize between 1 and 100
- Use `CancellationToken ct = default` for optional cancellation token

### Exception to HTTP Status Mapping
- `UnauthorizedAccessException` → `Results.Unauthorized()` (401)
- `KeyNotFoundException` → `Results.NotFound()` (404)
- General exceptions → `Results.Problem()` (500)

### File Download Endpoint
- Return `Results.File(stream, "application/octet-stream")` with generic octet-stream
- Service returns `Stream` directly from storage adapter

### Metadata Update Pattern
- Accept `string? metadata` as optional query parameter
- Return updated `UploadFileResponse` on success (200 OK)
