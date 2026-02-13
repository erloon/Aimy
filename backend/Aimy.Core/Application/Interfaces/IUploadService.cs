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
}
