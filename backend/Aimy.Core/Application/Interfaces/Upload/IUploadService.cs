using Aimy.Core.Application.DTOs;
using Aimy.Core.Application.DTOs.Upload;

namespace Aimy.Core.Application.Interfaces.Upload;

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
