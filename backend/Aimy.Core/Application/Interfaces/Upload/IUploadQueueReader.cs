using Aimy.Core.Application.DTOs.Upload;

namespace Aimy.Core.Application.Interfaces.Upload;

public interface IUploadQueueReader
{
    IAsyncEnumerable<UploadToProcess> ReadAllAsync(CancellationToken ct = default);
}