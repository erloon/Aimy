# Upload Feature Learnings

## 2026-02-13: Task 1 - Core Domain & Interfaces

### Created Files
- `backend/Aimy.Core/Domain/Entities/Upload.cs` - Upload entity (POCO)
- `backend/Aimy.Core/Application/Interfaces/IStorageService.cs` - Storage port
- `backend/Aimy.Core/Application/Interfaces/IUploadRepository.cs` - Repository port
- `backend/Aimy.Core/Application/Interfaces/ICurrentUserService.cs` - Current user port
- `backend/Aimy.Core/Application/DTOs/UploadFileResponse.cs` - Response DTO

### Patterns Used
- Entity constructor auto-generates `Id` with `Guid.NewGuid()`
- `DateUploaded` defaults to `DateTime.UtcNow` in constructor
- `required` keyword for mandatory properties
- `string?` for nullable strings (ContentType)
- `CancellationToken ct` parameter for async methods

### Core Purity Verified
- Build succeeded with 0 errors/warnings
- Only Microsoft.Extensions.* packages (standard .NET abstractions)
- No external dependencies added

## 2026-02-13: Task 3 - Service Layer & Unit Tests

### Created Files
- `backend/Aimy.Core/Application/Interfaces/IUploadService.cs` - Upload service port
- `backend/Aimy.Core/Application/Services/UploadService.cs` - Upload service implementation
- `backend/Aimy.Tests/Services/UploadServiceTests.cs` - Unit tests (3 tests)

### Modified Files
- `backend/Aimy.Core/DependencyInjection.cs` - Added IUploadService registration

### Service Logic Flow
1. Get current user ID via ICurrentUserService
2. Throw UnauthorizedAccessException if no user
3. Get file size from Stream.Length
4. Upload file via IStorageService.UploadAsync
5. Create Upload entity with returned storage path
6. Save via IUploadRepository.AddAsync
7. Map to UploadFileResponse and return

### Test Cases
1. `UploadAsync_ValidRequest_ReturnsResponse` - Happy path with mocks
2. `UploadAsync_NoCurrentUser_ThrowsUnauthorizedAccessException` - Auth failure
3. `UploadAsync_StorageFails_ThrowsException` - Error propagation

### Patterns Used
- Constructor injection of all ports (IStorageService, IUploadRepository, ICurrentUserService)
- Standard service pattern matching AuthService
- Moq with Verify for interaction assertions
- AAA pattern with Arrange/Act/Assert comments
- FluentAssertions for readable assertions

### Core Purity Verified
- Build succeeded with 0 errors/warnings
- Only Microsoft.Extensions.* packages
- No project references to Infrastructure
- All 3 tests pass

## 2026-02-13: Task 2 - Infrastructure Adapters

### Created Files
- `backend/Aimy.Infrastructure/Storage/MinioStorageService.cs` - MinIO adapter
- `backend/Aimy.Infrastructure/Repositories/UploadRepository.cs` - EF repository
- `backend/Aimy.Infrastructure/Security/CurrentUserService.cs` - HTTP context adapter
- `backend/Aimy.Infrastructure/Data/Configurations/UploadConfiguration.cs` - EF Fluent API

### Modified Files
- `backend/Aimy.Infrastructure/Data/ApplicationDbContext.cs` - Added `DbSet<Upload> Uploads`
- `backend/Aimy.Infrastructure/DependencyInjection.cs` - Registered new services
- `backend/Aimy.Infrastructure/Aimy.Infrastructure.csproj` - Added Minio + Http.Abstractions packages
- `backend/Aimy.API/Program.cs` - Added `AddHttpContextAccessor()`

### Migration Created
- `20260213155434_AddUploads` - Creates "uploads" table with index on UserId

### NuGet Packages Added
- `Minio` (7.0.0) - S3-compatible client
- `Microsoft.AspNetCore.Http.Abstractions` (2.3.9) - For IHttpContextAccessor

### MinioStorageService Pattern
1. Inject `IMinioClient` (registered by Aspire via `builder.AddMinioClient("storage")`)
2. Create bucket per user (bucket name = userId.ToString())
3. Generate unique object name: `{Guid}_{fileName}`
4. Upload stream with PutObjectAsync
5. Return storage path: `{bucketName}/{objectName}`

### CurrentUserService Pattern
- Inject `IHttpContextAccessor`
- Extract `ClaimTypes.NameIdentifier` from `HttpContext.User`
- Parse to Guid, return null if missing/invalid
- **Note:** `AddHttpContextAccessor()` must be called in API project (not Infrastructure)

### UploadConfiguration (EF Fluent API)
- Table: "uploads"
- Index on `UserId` for query performance
- MaxLength: FileName(500), StoragePath(1000), ContentType(256)
- All non-nullable properties marked `.IsRequired()`

### Dependency Injection Registration
```csharp
builder.Services.AddScoped<IUploadRepository, UploadRepository>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IStorageService, MinioStorageService>();
```

### Key Gotcha
- `AddHttpContextAccessor()` is part of ASP.NET Core shared framework
- Cannot be called in class library (Infrastructure) - must be in API's Program.cs

### Build Verification
- Build succeeded with 0 errors/warnings
- Migration created successfully


## 2026-02-13: Task 4 - API Layer & Integration

### Created Files
- `backend/Aimy.API/Validators/UploadFileRequestValidator.cs` - FluentValidation for IFormFile
- `backend/Aimy.API/Endpoints/UploadEndpoints.cs` - Upload endpoint (POST /upload)

### Modified Files
- `backend/Aimy.API/Program.cs` - Added FluentValidation registration + MapUploadEndpoints

### Package Added
- FluentValidation.AspNetCore 11.3.1 (to Aimy.API project)

### Endpoint Pattern
- Extension method on IEndpointRouteBuilder
- MapGroup with RequireAuthorization for auth
- DisableAntiforgery for multipart/form-data uploads
- Validation: size < 50MB, extensions [.txt, .docx, .md, .pdf]
- Return Results.Created on success, Results.Unauthorized on auth failure

### Build Verification
- Build succeeded with 0 warnings, 0 errors


## 2026-02-13: UI Requirements Gap Fix

### Missing Features Identified
1. **Metadata support** - Optional JSON string in request
2. **Duplicate filename handling** - Append suffix (e.g., `report.pdf` -> `report (1).pdf`)
3. **Response fields** - `contentType` and `metadata` missing from DTO

### Changes Made

#### Entity (Upload.cs)
- Added `string? Metadata` property for JSON metadata storage

#### DTO (UploadFileResponse.cs)
- Added `ContentType` property
- Added `Metadata` property
- Reordered properties to match UI spec

#### Repository (IUploadRepository, UploadRepository)
- Added `GetByUserIdAndFileNameAsync(userId, fileName, ct)` for duplicate checking

#### Service (UploadService.cs)
- Added `metadata` parameter to `UploadAsync`
- Added `GetUniqueFileNameAsync` method for duplicate handling
  - Checks existing files with same name
  - Appends ` (1)`, ` (2)`, etc. suffix
  - Safety limit of 1000, fallback to GUID suffix

#### API Endpoint (UploadEndpoints.cs)
- Added `string? metadata` parameter (from form field)
- Passes metadata to service

#### EF Configuration (UploadConfiguration.cs)
- Added `Metadata` column as `jsonb` type
- Added composite index on `(UserId, FileName)` for duplicate checking

#### Migration
- `20260213164143_AddUploadMetadata` - Adds Metadata column + composite index

#### Tests (UploadServiceTests.cs)
- Updated all tests for new metadata parameter
- Added test for duplicate filename handling: `UploadAsync_DuplicateFileName_AppendsSuffix`

### UI Response Format (Now Matching)
```json
{
  "id": "3fa85f64-5717...",
  "filename": "report (1).pdf",
  "contentType": "application/pdf",
  "sizeBytes": 1024,
  "uploadedAt": "2024-02-13T12:00:00Z",
  "link": "minio/...",
  "metadata": "{\"category\": \"invoice\"}"
}
```

### Build & Test Verification
- Build: 0 errors, 0 warnings
- Tests: 7/7 passed (added 1 new test for duplicate handling)
- Migration created successfully

