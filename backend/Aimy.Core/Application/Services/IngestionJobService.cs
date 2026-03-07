using Aimy.Core.Application.Configuration;
using Aimy.Core.Application.DTOs.Upload;
using Aimy.Core.Application.Interfaces.Upload;
using Aimy.Core.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aimy.Core.Application.Services;

public class IngestionJobService(
    IIngestionJobRepository ingestionJobRepository,
    IUploadRepository uploadRepository,
    IOptions<IngestionJobOptions> options,
    ILogger<IngestionJobService> logger) : IIngestionJobService
{
    private readonly IngestionJobOptions _options = options.Value;

    public async Task EnqueueAsync(Guid uploadId, CancellationToken ct)
    {
        var upload = await uploadRepository.GetByIdAsync(uploadId, ct);
        if (upload is not null)
        {
            upload.IngestionStatus = UploadIngestionStatus.Pending;
            upload.IngestionError = null;
            upload.IngestionCompletedAt = null;
            await uploadRepository.UpdateAsync(upload, ct);
        }

        await ingestionJobRepository.EnqueueIfNotExistsAsync(uploadId, ct);
    }

    public async Task<ClaimedIngestionJobDto?> ClaimNextAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var job = await ingestionJobRepository.ClaimNextPendingAsync(now, ct);
        if (job is null)
        {
            return null;
        }

        var upload = await uploadRepository.GetByIdAsync(job.UploadId, ct);
        if (upload is null)
        {
            await ingestionJobRepository.MarkFailedAsync(
                job.Id,
                $"Upload '{job.UploadId}' not found.",
                _options.MaxJobAttempts,
                now,
                now.AddSeconds(_options.RetryDelaySeconds),
                ct);
            return null;
        }

        upload.IngestionStatus = UploadIngestionStatus.Processing;
        upload.IngestionStartedAt = now;
        upload.IngestionCompletedAt = null;
        upload.IngestionError = null;
        await uploadRepository.UpdateAsync(upload, ct);

        return new ClaimedIngestionJobDto
        {
            JobId = job.Id,
            UploadId = job.UploadId,
            Attempts = job.Attempts
        };
    }

    public async Task MarkCompletedAsync(Guid jobId, Guid uploadId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await ingestionJobRepository.MarkCompletedAsync(jobId, now, ct);

        var upload = await uploadRepository.GetByIdAsync(uploadId, ct);
        if (upload is null)
        {
            return;
        }

        upload.IngestionStatus = UploadIngestionStatus.Completed;
        upload.IngestionError = null;
        upload.IngestionCompletedAt = now;
        await uploadRepository.UpdateAsync(upload, ct);
    }

    public async Task MarkFailedAsync(Guid jobId, Guid uploadId, Exception exception, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nextAttemptAt = now.AddSeconds(_options.RetryDelaySeconds);
        var error = exception.Message;

        await ingestionJobRepository.MarkFailedAsync(
            jobId,
            error,
            _options.MaxJobAttempts,
            now,
            nextAttemptAt,
            ct);

        var job = await ingestionJobRepository.GetByIdAsync(jobId, ct);
        var hasPendingRetry = job is not null && job.Status == IngestionJobStatus.Pending;

        var upload = await uploadRepository.GetByIdAsync(uploadId, ct);
        if (upload is null)
        {
            return;
        }

        upload.IngestionStatus = hasPendingRetry
            ? UploadIngestionStatus.Pending
            : UploadIngestionStatus.Failed;
        upload.IngestionError = error;
        upload.IngestionCompletedAt = hasPendingRetry ? null : now;
        await uploadRepository.UpdateAsync(upload, ct);

        logger.LogWarning(
            exception,
            "Ingestion job {JobId} failed for upload {UploadId}; pending retry: {HasPendingRetry}",
            jobId,
            uploadId,
            hasPendingRetry);
    }

    public async Task<IReadOnlyList<IngestionJobStatusResponse>> ListAsync(string? status, int limit, CancellationToken ct)
    {
        IngestionJobStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<IngestionJobStatus>(status, true, out var enumValue))
            {
                throw new ArgumentException($"Unknown ingestion job status '{status}'.", nameof(status));
            }

            parsedStatus = enumValue;
        }

        var sanitizedLimit = Math.Clamp(limit, 1, 200);
        var jobs = await ingestionJobRepository.ListAsync(parsedStatus, sanitizedLimit, ct);
        return jobs.Select(job => new IngestionJobStatusResponse
        {
            JobId = job.Id,
            UploadId = job.UploadId,
            Status = job.Status.ToString(),
            Attempts = job.Attempts,
            NextAttemptAt = job.NextAttemptAt,
            ClaimedAt = job.ClaimedAt,
            CompletedAt = job.CompletedAt,
            LastError = job.LastError,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt
        }).ToList();
    }

    public Task<bool> RetryAsync(Guid jobId, CancellationToken ct)
    {
        return ingestionJobRepository.RetryAsync(jobId, DateTime.UtcNow, ct);
    }
}
