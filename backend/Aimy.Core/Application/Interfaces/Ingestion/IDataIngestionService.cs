namespace Aimy.Core.Application.Interfaces.Ingestion;

using Aimy.Core.Application.DTOs.Upload;

public interface IDataIngestionService
{
    Task IngestDataAsync(Guid uploadId, CancellationToken cancellationToken);

    Task DeleteByUploadIdAsync(Guid uploadId, CancellationToken cancellationToken);

    Task<UploadIngestionResponse?> GetByUploadIdAsync(Guid uploadId, CancellationToken cancellationToken);
}
