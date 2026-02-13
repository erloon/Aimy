namespace Aimy.Core.Application.Services;

using Aimy.Core.Application.DTOs;
using Aimy.Core.Application.Interfaces;
using Aimy.Core.Domain.Entities;

public class UploadService : IUploadService
{
    private readonly IStorageService _storageService;
    private readonly IUploadRepository _uploadRepository;
    private readonly ICurrentUserService _currentUserService;

    public UploadService(
        IStorageService storageService,
        IUploadRepository uploadRepository,
        ICurrentUserService currentUserService)
    {
        _storageService = storageService;
        _uploadRepository = uploadRepository;
        _currentUserService = currentUserService;
    }

    public async Task<UploadFileResponse> UploadAsync(
        Stream fileStream,
        string fileName,
        string? contentType,
        string? metadata,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (userId is null)
            throw new UnauthorizedAccessException("User is not authenticated");

        // Handle duplicate filenames - append suffix if file with same name exists
        var displayFileName = await GetUniqueFileNameAsync(userId.Value, fileName, ct);

        var fileSizeBytes = fileStream.Length;
        var storagePath = await _storageService.UploadAsync(
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

        var savedUpload = await _uploadRepository.AddAsync(upload, ct);

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

    private async Task<string> GetUniqueFileNameAsync(Guid userId, string originalFileName, CancellationToken ct)
    {
        var existingUploads = await _uploadRepository.GetByUserIdAndFileNameAsync(userId, originalFileName, ct);
        
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
            var duplicates = await _uploadRepository.GetByUserIdAndFileNameAsync(userId, newFileName, ct);
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
