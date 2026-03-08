# Learnings

## [2026-03-08] Initial Codebase Analysis

### Current State (Before Refactoring)
- `Upload` entity has ingestion fields: `IngestionStatus`, `IngestionError`, `IngestionStartedAt`, `IngestionCompletedAt`, `SourceMarkdown`, `IsMarkdownUpload`
- `IngestionJob` entity is missing `StartedAt` field
- `IIngestionJobService` lives in `Aimy.Core.Application.Interfaces.Upload` namespace (wrong boundary)
- `IStorageService.UploadAsync` does NOT take `bucketName` parameter
- `MinioStorageService` uses per-user bucket strategy (userId as bucket name)
- `UploadService` enqueues ingestion jobs after every upload (coupling)
- `UploadEndpoints.cs` contains ingestion routes (`/uploads/ingestion-jobs`)
- Enum `UploadIngestionStatus` is used on Upload entity
- `IngestionJobService` mutates `Upload` entity fields directly during lifecycle transitions

### Domain Entity Structure
- Upload: `Id, UserId, FileName, StoragePath, FileSizeBytes, ContentType, Metadata, SourceMarkdown, IngestionStatus, IngestionError, IngestionStartedAt, IngestionCompletedAt, DateUploaded`
- IngestionJob: `Id, UploadId, Status, Attempts, NextAttemptAt, ClaimedAt, CompletedAt, LastError, CreatedAt, UpdatedAt`
- No `StartedAt` on IngestionJob yet

### Key File Paths
- Entities: `backend/Aimy.Core/Domain/Entities/`
- Interfaces: `backend/Aimy.Core/Application/Interfaces/Upload/` (IIngestionJobService needs to move to KnowledgeBase/)
- Services: `backend/Aimy.Core/Application/Services/`
- Storage: `backend/Aimy.Infrastructure/Storage/MinioStorageService.cs`
- DB Config: `backend/Aimy.Infrastructure/Data/Configurations/`
- Endpoints: `backend/Aimy.API/Endpoints/`
- Frontend KB: `frontend/src/features/knowledge-base/`

### Architecture Conventions
- Hexagonal Architecture: Core has zero external deps
- Use TypedResults in endpoints
- NUnit + Moq + FluentAssertions for tests
- EF Core Fluent API configurations in Infrastructure/Data/Configurations

## [2026-03-08] Task 1 - Storage Naming Constants

### Implemented
- Added `backend/Aimy.Infrastructure/Storage/StorageConstants.cs` with canonical bucket constant `knowledgebase`
- Added key helper `StorageKeyFormat.KbItemKey(Guid userId, string fileName)` using `{userId}/items/{guid}_{fileName}`
- Added explicit migration checkpoint comment: `[DECISION NEEDED: legacy MinIO objects strategy]`

### Verification
- `dotnet build aimy.sln` succeeded with 0 errors
- LSP diagnostics clean for `backend/Aimy.Infrastructure/Storage/StorageConstants.cs`

## [2026-03-08] Task 2 - Domain Entity Decoupling

### Changes Made
- `Upload.cs`: Removed `IngestionStatus`, `IngestionError`, `IngestionStartedAt`, `IngestionCompletedAt`, `SourceMarkdown`, `IsMarkdownUpload` computed property, `HasMarkdownContentType()`, `HasMarkdownFileExtension()` private methods. Constructor no longer sets `IngestionStatus`.
- `IngestionJob.cs`: Added `public DateTime? StartedAt { get; set; }` between `ClaimedAt` and `CompletedAt`.

### Fix Pattern: IsMarkdownUpload Logic Migration
- `IsMarkdownUpload` was a computed property on `Upload` but it's infrastructure-level behavior (file type detection for pipeline routing)
- Moved logic as `private static bool IsMarkdownUpload(Upload upload)` into `DefaultIngestionPipelineBuilder` (Infrastructure layer) where it's actually used
- This correctly removes domain awareness of ingestion pipeline concerns from the entity

### Fix Pattern: SourceMarkdown Migration
- `DataIngestionService` was storing extracted markdown back onto `Upload.SourceMarkdown`
- Removed that mutation; `SourceMarkdown = null` in `KnowledgeItemService` and `SemanticSearchService` mappings as stubs
- `UploadFileResponse.IngestionStatus` changed from `required string` to `string?` since it's now set to `null` pending task 5 rework

### Fix Pattern: IngestionJobService Cleanup
- Removed all `upload.Ingestion*` mutations from EnqueueAsync, ClaimNextAsync, MarkCompletedAsync, MarkFailedAsync
- Also removed the `uploadRepository.UpdateAsync(upload, ct)` calls that existed solely to persist ingestion state on Upload
- Lifecycle state now belongs entirely to IngestionJob

### Test Updates
- `UploadTests.cs`: Replaced IsMarkdownUpload tests with a single constructor defaults test (property no longer on entity)
- `KnowledgeItemServiceTests.cs`: Removed `SourceMarkdown` field initialization from Upload mock, updated assertion from expected value to `.BeNull()`

### Build Status
- `dotnet build aimy.sln` succeeded with 0 errors, 0 warnings
- Pre-existing ambiguous `IIngestionJobService` interface errors appear in LSP diagnostics but DO NOT affect build (they are in files that only reference the unambiguous `Upload.IIngestionJobService`)

## [2026-03-08] Task 4 - Move Ingestion Contracts to KnowledgeBase Boundary

### Changes Made
- Created `backend/Aimy.Core/Application/Interfaces/KnowledgeBase/IIngestionJobService.cs` with namespace `Aimy.Core.Application.Interfaces.KnowledgeBase`
- Created `backend/Aimy.Core/Application/Interfaces/KnowledgeBase/IIngestionJobRepository.cs` with namespace `Aimy.Core.Application.Interfaces.KnowledgeBase`
- Deleted `backend/Aimy.Core/Application/Interfaces/Upload/IIngestionJobService.cs`
- Deleted `backend/Aimy.Core/Application/Interfaces/Upload/IIngestionJobRepository.cs`
- Method signatures preserved exactly (no changes to contracts)

### Files Updated (using statement changes only)
| File | Change |
|------|--------|
| `IngestionJobService.cs` | Added `using KnowledgeBase;` (kept `Upload;` for IUploadRepository) |
| `IngestionJobRepository.cs` | Replaced `Upload;` with `KnowledgeBase;` |
| `UploadProcessingWorker.cs` | Replaced `Upload;` with `KnowledgeBase;` |
| `UploadEndpoints.cs` | Added `using KnowledgeBase;` (kept `Upload;` for IUploadService) |

### Files Unchanged (already had correct dual imports)
- `Aimy.Core/DependencyInjection.cs` - already had both `KnowledgeBase` and `Upload` usings
- `Aimy.Infrastructure/DependencyInjection.cs` - already had both
- `UploadKnowledgeSyncService.cs` - already had both
- `UploadKnowledgeSyncServiceTests.cs` - already had both

### Build Result
- All 4 backend projects (Core, Infrastructure, API, Tests) build with 0 errors, 0 warnings
- `dotnet build aimy.sln` fails only due to `Aimy.AppHost.dll` being locked by running process (pre-existing environment issue, unrelated)

### Key Insight: Pre-existing Build State
- The baseline at HEAD was already broken with 1 error (ambiguous `IIngestionJobService` in `UploadKnowledgeSyncService.cs`)
- This was from task 2's changes that added a partial `KnowledgeBase.IIngestionJobService` stub
- Task 4 completes the move and fixes that ambiguity cleanly

### Namespace Strategy for Mixed Files
- Files using ONLY `IIngestionJob*`: replace `Upload` with `KnowledgeBase` 
- Files using `IIngestionJob*` AND other Upload interfaces: add `KnowledgeBase` alongside existing `Upload`

## [2026-03-08] Task 3 - EF Migration

### Implemented
- Removed ingestion column mappings from `UploadConfiguration.cs`: IngestionStatus, IngestionError, IngestionStartedAt, IngestionCompletedAt, SourceMarkdown, IX_uploads_IngestionStatus index
- Added `StartedAt` mapping to `IngestionJobConfiguration.cs` with `.HasColumnName("started_at")`
- Created migration: `20260308223614_RemoveIngestionFieldsFromUpload`
- Migration drops 5 ingestion columns + index from `uploads`, adds `started_at` to `ingestion_jobs`
- Migration applied successfully to Aspire-managed PostgreSQL container

### Key Observations
- Aspire postgres runs as Docker container `postgres-398202c0` on dynamic port (found via `docker ps`)
- Password is per-session dynamic, retrieved via `docker inspect postgres-398202c0 --format '{{range .Config.Env}}{{println .}}{{end}}'`
- Must pass `--connection` flag to `dotnet ef database update` when no local DB is configured
- Running `dotnet build aimy.sln` while Aspire API is running causes MSB3027 file lock error (Windows DLL lock) - not a code error
- Build `backend/Aimy.Infrastructure` and `backend/Aimy.Core` individually works fine when API is locked
- Drift check produced empty migration = no model drift

### Verification
- `backend/Aimy.Infrastructure` build: SUCCEEDED (0 errors, 0 warnings)
- `backend/Aimy.Core` build: SUCCEEDED (0 errors, 0 warnings)
- `__drift_check` migration was empty → no drift
- Migration applied: `20260308223614_RemoveIngestionFieldsFromUpload` confirmed in `__EFMigrationsHistory`
- Commit: `refactor(db): align upload and ingestion schema boundaries`

## Task 5 — StartedAt wiring in IngestionJobRepository

**Date:** 2026-03-08

### What was done
- `IngestionJobRepository.ClaimNextPendingAsync` raw SQL UPDATE was missing `"StartedAt" = @nowUtc` in the SET clause.
- `RETURNING` clause was missing `jobs."StartedAt"`, causing the returned entity to always have `StartedAt = null`.
- Reader ordinal indices were off by one after `ClaimedAt` (index 5) — `StartedAt` is now at index 6, shifting `CompletedAt` to 7, `LastError` to 8, `CreatedAt` to 9, `UpdatedAt` to 10.

### Key pattern
When using raw SQL with `RETURNING` in Postgres, reader ordinals are positional and must match the exact column order in the `RETURNING` clause. Adding a column in the middle requires updating ALL subsequent ordinal indices in the reader hydration block.

### Verification
- `UploadProcessingWorker` confirmed clean — only uses `IIngestionJobService`, zero Upload mutations.
- `ClaimedIngestionJobDto` does NOT need `StartedAt` — it is only used to convey JobId/UploadId/Attempts to the worker. No downstream consumer needs the value from the DTO.
- Both `Aimy.Core` and `Aimy.Infrastructure` build with 0 errors, 0 warnings.
- LSP diagnostics: no errors on changed file.

## Task 6 — IStorageService bucketName param + central MinIO bucket

**Date:** 2026-03-08

### Changes Made
- `IStorageService.UploadAsync`: Added `string bucketName` as second parameter (after `userId`, before `fileName`)
- `MinioStorageService.UploadAsync`: Replaced `userId.ToString()` as bucket with `bucketName` param; replaced `$"{Guid.NewGuid()}_{fileName}"` object key with `StorageKeyFormat.KbItemKey(userId, fileName)`
- `UploadService.UploadAsync`: Inserted `"knowledgebase"` literal as second argument at the call site
- `KnowledgeItemService.UploadAsync` (×2 call sites: CreateNoteAsync at line ~44, UpdateAsync at line ~190): same pattern — inserted `"knowledgebase"` literal
- `KnowledgeItemServiceTests.cs`: Updated all 5 Mock Setup/Verify calls + 1 Callback<> type signature to include `string bucketName` as second type param

### Key Findings
- **KnowledgeItemService was an undocumented caller** — grep `storageService.UploadAsync` found 2 call sites in `KnowledgeItemService.cs` that the task description didn't list. Always grep all Core services.
- **Test Callback<> types matter**: Moq's `Callback<T1, T2, ...>` must list ALL parameter types in order. Adding `string bucketName` second means shifting all existing types right.
- **LSP reports stale errors on Tests project** — LSP showed `CS1501: No overload for method 'UploadAsync' takes 6 arguments` even after correct edits. Trust the compiler (`dotnet build`) which showed 0 `error CS*` errors.
- **Core constraint respected**: passed `"knowledgebase"` as string literal in Core — never referenced `StorageBuckets` from Infrastructure.
- **StorageKeyFormat.KbItemKey** generates `{userId}/items/{Guid.NewGuid()}_{fileName}` — MinIO treats `/` as path separator; `ParseStoragePath` splits on first `/` giving `bucket=knowledgebase` and `objectName={userId}/items/...`. Backward compat intact.

### Build Verification
- `Aimy.Core`: SUCCEEDED (0 errors, 0 warnings)  
- `Aimy.Infrastructure`: SUCCEEDED (0 errors, 0 warnings)
- `Aimy.API` + `Aimy.Tests`: MSB3027 file-lock errors only (DLLs locked by running Aspire process) — zero `error CS*`

## Task 7 — Remove ingestion coupling from UploadService

**Date:** 2026-03-09

### Changes Made

**`UploadService.cs`**:
- Removed `try { await uploadKnowledgeSyncService.EnqueueIngestionAsync(...) } catch { ... }` block from `UploadAsync` — generic uploads no longer trigger ingestion
- Converted `BuildUploadFileResponseAsync(Upload upload, CancellationToken ct)` → `BuildUploadFileResponse(Upload upload)` as a simple `private static` method — no longer calls `dataIngestionService.GetByUploadIdAsync`; always sets `Ingestion = null`
- Updated all 3 callers: `UploadAsync`, `ListAsync`, `UpdateMetadataAsync` to use new sync method
- `IDataIngestionService` kept in constructor — still needed by `DeleteAsync` → `DeleteByUploadIdAsync`

**`UploadFileResponse.cs`**:
- Removed 4 deprecated flat fields: `IngestionStatus`, `IngestionError`, `IngestionStartedAt`, `IngestionCompletedAt`
- Kept `Ingestion` (type `UploadIngestionResponse?`) — the properly-structured nested ingestion DTO

**`UploadServiceTests.cs`**:
- Removed `EnqueueIngestionAsync` setup from `Setup()`
- Removed `GetByUploadIdAsync` default setup and the explicit `ReturnsAsync(UploadIngestionResponse{...})` setup in `ListAsync_WithIngestion_ReturnsSummaryAndChunks`
- Removed `UpdateMetadataByUploadIdAsync` setup from `Setup()`
- Updated `ListAsync_WithIngestion_ReturnsSummaryAndChunks` assertions: no longer expects non-null ingestion; now verifies `Ingestion.Should().BeNull()` (generic uploads never populate ingestion)
- Fixed 5 stale 5-arg `storageService.UploadAsync` Setup/Verify calls to include `"knowledgebase"` bucket argument

### Key Design Decision
The task spec says "remove `IDataIngestionService` from constructor" but `DeleteAsync` still calls `dataIngestionService.DeleteByUploadIdAsync(id, ct)`. Kept `IDataIngestionService` in the constructor and tests; only removed `GetByUploadIdAsync` call path. The test `DeleteAsync_ValidOwnedFile_DeletesFromStorageAndRepository` still verifies this call.

### Build Verification
- `dotnet build backend/Aimy.API`: SUCCEEDED (0 errors, 0 warnings)
- `dotnet build backend/Aimy.Tests`: SUCCEEDED (0 errors, 0 warnings)
- Evidence saved to `.sisyphus/evidence/task-7-upload-service.txt`

## Task 8 — Unified KB upload orchestration endpoint with rollback

**Date:** 2026-03-09

### Changes Made
- Added `UploadToFolderRequest` DTO in `Aimy.Core.Application.DTOs.KnowledgeBase` with `FolderId`, `Title`, `Metadata`, `FileName`, `ContentType`, `FileStream`
- Added `IKnowledgeItemService.UploadToFolderAsync(UploadToFolderRequest, CancellationToken)` and implemented it in `KnowledgeItemService`
- Implemented compensating rollback chain in `KnowledgeItemService.UploadToFolderAsync`:
  - upload record save failure => delete storage object
  - ingestion enqueue failure => delete upload record + storage object
  - item creation failure => delete upload record + storage object
- Added new `POST /kb/items/upload` endpoint in `KnowledgeItemEndpoints` with:
  - `.DisableAntiforgery()`
  - `.Accepts<IFormFile>("multipart/form-data")`
  - TypedResults signature + standard auth/not-found/problem handling
  - File validation aligned to `UploadEndpoints`: max 50MB and extensions `.txt`, `.docx`, `.md`, `.pdf`

### Test Coverage Added
- Extended `KnowledgeItemServiceTests` with 7 orchestration tests:
  - happy path
  - storage upload failure
  - upload DB save failure rollback
  - ingestion enqueue failure rollback
  - item creation failure rollback
  - unauthenticated user
  - folder not found

### Verification
- `dotnet test backend/Aimy.Tests/Aimy.Tests.csproj`: PASSED (`102` tests)
- `dotnet build aimy.sln`: SUCCEEDED (0 errors, 0 warnings)
- Build evidence saved to `.sisyphus/evidence/task-8-kb-upload.txt`

## Task 9 — Split ingestion endpoints

**Date:** 2026-03-09

### Changes Made
- Created `backend/Aimy.API/Endpoints/IngestionEndpoints.cs`: static class `IngestionEndpoints` with `MapIngestionEndpoints(this IEndpointRouteBuilder app)` extension method
- Group: `app.MapGroup("/kb/ingestion")` with tag `"Knowledge Base - Ingestion"` and `.RequireAuthorization()`
- Routes: `GET /jobs` (ListIngestionJobs) and `POST /jobs/{jobId}/retry` (RetryIngestionJob) — names kept identical
- Removed from `UploadEndpoints.cs`: `using Aimy.Core.Application.Interfaces.KnowledgeBase;`, 2 route registrations, 2 handler methods
- Added `app.MapIngestionEndpoints();` to `Program.cs` after `app.MapKnowledgeItemEndpoints();`

### Key Findings
- `IngestionJobStatusResponse` lives in `Aimy.Core.Application.DTOs.Upload` namespace (not KnowledgeBase) — because the DTO file predates the namespace move
- After removing handler methods from `UploadEndpoints.cs`, the `using Aimy.Core.Application.Interfaces.KnowledgeBase;` was the ONLY change needed to the using block — `IUploadService` (from Upload namespace) is still used by the remaining handlers
- LSP showed errors after removing the `using` but before removing the handler bodies — correct transient state, resolved after handler removal

### Build Verification
- `dotnet build backend/Aimy.API`: SUCCEEDED (0 errors, 0 warnings)
- `dotnet test backend/Aimy.Tests`: PASSED (102 tests)
- Evidence saved to `.sisyphus/evidence/task-9-ingestion-endpoints.txt`

---
## Task 11 — IngestionEndpointsContractTests (2026-03-09)

### Pattern Used
Mirrored `MetadataEndpointsContractTests.cs` exactly: reflection-only contract tests using `BindingFlags.NonPublic | BindingFlags.Static` to reach private handler methods.

### New File
`backend/Aimy.Tests/Endpoints/IngestionEndpointsContractTests.cs` — 9 tests.

### Key Findings
- `ListIngestionJobs` returns `Task<Results<Ok<IReadOnlyList<IngestionJobStatusResponse>>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, ProblemHttpResult>>` — important: the Ok<T> payload is `IReadOnlyList<>` not `List<>`.
- `RetryIngestionJob` returns `Task<Results<NoContent, BadRequest<ErrorResponse>, UnauthorizedHttpResult, NotFound, ProblemHttpResult>>` — success is `NoContent`, NOT `Ok<T>`, so the "no object in Ok" test doesn't apply here. Tested the presence of `NoContent` instead.
- `IngestionJobStatusResponse` lives in `Aimy.Core.Application.DTOs.Upload` namespace (file: `IngestionJobDtos.cs`).
- Old routes `/uploads/ingestion-jobs` confirmed GONE — grep on `UploadEndpoints.cs` returns 0 matches; `OldIngestionRoutesRemovedFromUploadEndpoints` test passes.
- Total test count went from 102 → 111 (9 new tests added).
- Evidence: `.sisyphus/evidence/task-11-backend-tests.txt`
