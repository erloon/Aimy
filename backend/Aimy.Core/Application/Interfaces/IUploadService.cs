namespace Aimy.Core.Application.Interfaces;

using Aimy.Core.Application.DTOs;

public interface IUploadService
{
    Task<UploadFileResponse> UploadAsync(
        Stream fileStream,
        string fileName,
        string? contentType,
        string? metadata,
        CancellationToken ct);

    Task<PagedResult<UploadFileResponse>> ListAsync(
        int page,
        int pageSize,
        CancellationToken ct);

    Task<Stream> DownloadAsync(Guid id, CancellationToken ct);

    Task DeleteAsync(Guid id, CancellationToken ct);

    Task<UploadFileResponse> UpdateMetadataAsync(Guid id, string? metadata, CancellationToken ct);
}
