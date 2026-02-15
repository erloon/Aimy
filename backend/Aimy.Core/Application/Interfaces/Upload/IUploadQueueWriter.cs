using Aimy.Core.Application.DTOs.Upload;

namespace Aimy.Core.Application.Interfaces.Upload;

public interface IUploadQueueWriter
{
    Task WriteAsync(UploadToProcess upload, CancellationToken ct = default);
}