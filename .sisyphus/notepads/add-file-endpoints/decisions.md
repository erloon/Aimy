# Decisions - Add File Endpoints

## 2026-02-13: Task 3 - Service Logic

- Use strict ownership enforcement for `DownloadAsync`, `DeleteAsync`, and `UpdateMetadataAsync`: unauthenticated -> `UnauthorizedAccessException`, missing file -> `KeyNotFoundException`, non-owner -> `UnauthorizedAccessException`
- Keep metadata update as full replacement of the metadata JSON string (`upload.Metadata = metadata`) for this task scope
- Execute delete in storage-first order (`IStorageService.DeleteAsync`) before repository delete (`IUploadRepository.DeleteAsync`) to avoid DB-first orphaning when storage deletion fails
