using Aimy.Core.Application.Configuration;
using Aimy.Core.Application.DTOs.Upload;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
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

        logger.LogWarning(
            exception,
            "Ingestion job {JobId} failed for upload {UploadId}; pending retry: {HasPendingRetry}",
            jobId,
            uploadId,
            job is not null && job.Status == IngestionJobStatus.Pending);
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
