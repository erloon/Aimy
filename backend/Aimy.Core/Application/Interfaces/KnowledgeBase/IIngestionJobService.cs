using Aimy.Core.Application.DTOs.Upload;

namespace Aimy.Core.Application.Interfaces.KnowledgeBase;

public interface IIngestionJobService
{
    Task EnqueueAsync(Guid uploadId, CancellationToken ct);
    Task<ClaimedIngestionJobDto?> ClaimNextAsync(CancellationToken ct);
    Task MarkCompletedAsync(Guid jobId, Guid uploadId, CancellationToken ct);
    Task MarkFailedAsync(Guid jobId, Guid uploadId, Exception exception, CancellationToken ct);
    Task<IReadOnlyList<IngestionJobStatusResponse>> ListAsync(string? status, int limit, CancellationToken ct);
    Task<bool> RetryAsync(Guid jobId, CancellationToken ct);
}
