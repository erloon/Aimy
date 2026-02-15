using Aimy.Core.Application.DTOs.Upload;
using Aimy.Core.Application.Interfaces.Auth;
using Aimy.Core.Application.Interfaces.Upload;

namespace Aimy.Core.Application.Services;

using DTOs;
using Domain.Entities;

public class UploadService(
    IStorageService storageService,
    IUploadRepository uploadRepository,
    ICurrentUserService currentUserService,
    IUploadQueueWriter queueWriter)
    : IUploadService
{
    public async Task<UploadFileResponse> UploadAsync(
        Stream fileStream,
        string fileName,
        string? contentType,
        string? metadata,
        CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId();
        if (userId is null)
            throw new UnauthorizedAccessException("User is not authenticated");

        // Handle duplicate filenames - append suffix if file with same name exists
        var displayFileName = await GetUniqueFileNameAsync(userId.Value, fileName, ct);

        var fileSizeBytes = fileStream.Length;
        var storagePath = await storageService.UploadAsync(
            userId.Value,
            displayFileName,
            fileStream,
            contentType,
            ct);

        var upload = new Upload
        {
            UserId = userId.Value,
            FileName = displayFileName,
            StoragePath = storagePath,
            FileSizeBytes = fileSizeBytes,
            ContentType = contentType,
            Metadata = metadata
        };

        var savedUpload = await uploadRepository.AddAsync(upload, ct);  
        await queueWriter.WriteAsync(new UploadToProcess(savedUpload.Id), ct);
        return new UploadFileResponse
        {
            Id = savedUpload.Id,
            FileName = savedUpload.FileName,
            ContentType = savedUpload.ContentType,
            Link = savedUpload.StoragePath,
            SizeBytes = savedUpload.FileSizeBytes,
            UploadedAt = savedUpload.DateUploaded,
            Metadata = savedUpload.Metadata
        };
    }

    public async Task<PagedResult<UploadFileResponse>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId();
        if (userId is null)
            throw new UnauthorizedAccessException("User is not authenticated");

        var pagedUploads = await uploadRepository.GetPagedAsync(userId.Value, page, pageSize, ct);

        return new PagedResult<UploadFileResponse>
        {
            Items = pagedUploads.Items.Select(MapToUploadFileResponse).ToList(),
            Page = pagedUploads.Page,
            PageSize = pagedUploads.PageSize,
            TotalCount = pagedUploads.TotalCount
        };
    }

    public async Task<Stream> DownloadAsync(Guid id, CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId();
        if (userId is null)
            throw new UnauthorizedAccessException("User is not authenticated");

        var upload = await uploadRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("File not found");

        if (upload.UserId != userId.Value)
            throw new UnauthorizedAccessException("User does not have access to this file");

        return await storageService.DownloadAsync(upload.StoragePath, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId();
        if (userId is null)
            throw new UnauthorizedAccessException("User is not authenticated");

        var upload = await uploadRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("File not found");

        if (upload.UserId != userId.Value)
            throw new UnauthorizedAccessException("User does not have access to this file");

        await storageService.DeleteAsync(upload.StoragePath, ct);
        await uploadRepository.DeleteAsync(id, ct);
    }

    public async Task<UploadFileResponse> UpdateMetadataAsync(Guid id, string? metadata, CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId();
        if (userId is null)
            throw new UnauthorizedAccessException("User is not authenticated");

        var upload = await uploadRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("File not found");

        if (upload.UserId != userId.Value)
            throw new UnauthorizedAccessException("User does not have access to this file");

        upload.Metadata = metadata;
        await uploadRepository.UpdateAsync(upload, ct);

        return MapToUploadFileResponse(upload);
    }

    private static UploadFileResponse MapToUploadFileResponse(Upload upload)
    {
        return new UploadFileResponse
        {
            Id = upload.Id,
            FileName = upload.FileName,
            ContentType = upload.ContentType,
            Link = upload.StoragePath,
            SizeBytes = upload.FileSizeBytes,
            UploadedAt = upload.DateUploaded,
            Metadata = upload.Metadata
        };
    }

    private async Task<string> GetUniqueFileNameAsync(Guid userId, string originalFileName, CancellationToken ct)
    {
        var existingUploads = await uploadRepository.GetByUserIdAndFileNameAsync(userId, originalFileName, ct);
        
        if (!existingUploads.Any())
        {
            return originalFileName;
        }

        // File with same name exists - generate unique name with suffix
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
        var extension = Path.GetExtension(originalFileName);
        var suffix = 1;

        string newFileName;
        do
        {
            newFileName = $"{fileNameWithoutExt} ({suffix}){extension}";
            var duplicates = await uploadRepository.GetByUserIdAndFileNameAsync(userId, newFileName, ct);
            if (!duplicates.Any())
            {
                return newFileName;
            }
            suffix++;
        } while (suffix < 1000); // Safety limit

        // Fallback with GUID if we somehow exceed limit
        return $"{fileNameWithoutExt}_{Guid.NewGuid():N}{extension}";
    }
}
