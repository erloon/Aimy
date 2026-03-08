using Aimy.Core.Domain.Entities;

namespace Aimy.Core.Application.Interfaces.Upload;

public interface IIngestionJobRepository
{
    Task<IngestionJob?> GetByIdAsync(Guid jobId, CancellationToken ct);
    Task EnqueueIfNotExistsAsync(Guid uploadId, CancellationToken ct);
    Task<IngestionJob?> ClaimNextPendingAsync(DateTime nowUtc, CancellationToken ct);
    Task MarkCompletedAsync(Guid jobId, DateTime completedAtUtc, CancellationToken ct);
    Task MarkFailedAsync(
        Guid jobId,
        string error,
        int maxAttempts,
        DateTime nowUtc,
        DateTime nextAttemptAtUtc,
        CancellationToken ct);
    Task<IReadOnlyList<IngestionJob>> ListAsync(IngestionJobStatus? status, int limit, CancellationToken ct);
    Task<bool> RetryAsync(Guid jobId, DateTime nextAttemptAtUtc, CancellationToken ct);
}
