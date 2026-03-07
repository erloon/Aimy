namespace Aimy.Core.Application.Interfaces.Upload;

public interface IUploadKnowledgeSyncService
{
    string? NormalizeMetadataPayload(string? metadata);
    Task SyncMetadataAsync(Aimy.Core.Domain.Entities.Upload upload, string? metadata, CancellationToken ct);
    Task EnqueueIngestionAsync(Guid uploadId, CancellationToken ct);
    Task ReingestAsync(Guid uploadId, CancellationToken ct);
}
