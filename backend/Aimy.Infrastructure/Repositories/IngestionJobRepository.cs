using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Domain.Entities;
using Aimy.Infrastructure.Data;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Aimy.Infrastructure.Repositories;

public class IngestionJobRepository(ApplicationDbContext context) : IIngestionJobRepository
{
    public async Task<IngestionJob?> GetByIdAsync(Guid jobId, CancellationToken ct)
    {
        return await context.IngestionJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
    }

    public async Task EnqueueIfNotExistsAsync(Guid uploadId, CancellationToken ct)
    {
        var hasActiveJob = await context.IngestionJobs
            .AnyAsync(
                j => j.UploadId == uploadId
                    && (j.Status == IngestionJobStatus.Pending || j.Status == IngestionJobStatus.Processing),
                ct);

        if (hasActiveJob)
        {
            return;
        }

        var existingFailedJob = await context.IngestionJobs
            .Where(j => j.UploadId == uploadId && j.Status == IngestionJobStatus.Failed)
            .OrderByDescending(j => j.UpdatedAt)
            .FirstOrDefaultAsync(ct);

        if (existingFailedJob is not null)
        {
            existingFailedJob.Status = IngestionJobStatus.Pending;
            existingFailedJob.LastError = null;
            existingFailedJob.ClaimedAt = null;
            existingFailedJob.CompletedAt = null;
            existingFailedJob.NextAttemptAt = DateTime.UtcNow;
            existingFailedJob.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
            return;
        }

        var job = new IngestionJob
        {
            UploadId = uploadId,
            Status = IngestionJobStatus.Pending,
            NextAttemptAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.IngestionJobs.Add(job);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IngestionJob?> ClaimNextPendingAsync(DateTime nowUtc, CancellationToken ct)
    {
        var connection = context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(ct);
        }

        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                WITH candidate AS (
                    SELECT ""Id""
                    FROM ingestion_jobs
                    WHERE ""Status"" = @pendingStatus
                      AND ""NextAttemptAt"" <= @nowUtc
                    ORDER BY ""CreatedAt"" ASC
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1
                )
                UPDATE ingestion_jobs AS jobs
                SET ""Status"" = @processingStatus,
                    ""ClaimedAt"" = @nowUtc,
                    ""UpdatedAt"" = @nowUtc,
                    ""LastError"" = NULL
                FROM candidate
                WHERE jobs.""Id"" = candidate.""Id""
                RETURNING
                    jobs.""Id"",
                    jobs.""UploadId"",
                    jobs.""Status"",
                    jobs.""Attempts"",
                    jobs.""NextAttemptAt"",
                    jobs.""ClaimedAt"",
                    jobs.""CompletedAt"",
                    jobs.""LastError"",
                    jobs.""CreatedAt"",
                    jobs.""UpdatedAt"";";

            var pendingStatusParameter = command.CreateParameter();
            pendingStatusParameter.ParameterName = "@pendingStatus";
            pendingStatusParameter.Value = (int)IngestionJobStatus.Pending;
            command.Parameters.Add(pendingStatusParameter);

            var processingStatusParameter = command.CreateParameter();
            processingStatusParameter.ParameterName = "@processingStatus";
            processingStatusParameter.Value = (int)IngestionJobStatus.Processing;
            command.Parameters.Add(processingStatusParameter);

            var nowParameter = command.CreateParameter();
            nowParameter.ParameterName = "@nowUtc";
            nowParameter.Value = nowUtc;
            command.Parameters.Add(nowParameter);

            IngestionJob? claimedJob = null;
            await using (var reader = await command.ExecuteReaderAsync(ct))
            {
                if (await reader.ReadAsync(ct))
                {
                    claimedJob = new IngestionJob
                    {
                        Id = reader.GetGuid(0),
                        UploadId = reader.GetGuid(1),
                        Status = (IngestionJobStatus)reader.GetInt32(2),
                        Attempts = reader.GetInt32(3),
                        NextAttemptAt = reader.GetDateTime(4),
                        ClaimedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                        CompletedAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                        LastError = reader.IsDBNull(7) ? null : reader.GetString(7),
                        CreatedAt = reader.GetDateTime(8),
                        UpdatedAt = reader.GetDateTime(9)
                    };
                }
            }

            await transaction.CommitAsync(ct);
            return claimedJob;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task MarkCompletedAsync(Guid jobId, DateTime completedAtUtc, CancellationToken ct)
    {
        var job = await context.IngestionJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null)
        {
            return;
        }

        job.Status = IngestionJobStatus.Completed;
        job.CompletedAt = completedAtUtc;
        job.ClaimedAt = null;
        job.LastError = null;
        job.UpdatedAt = completedAtUtc;
        await context.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(
        Guid jobId,
        string error,
        int maxAttempts,
        DateTime nowUtc,
        DateTime nextAttemptAtUtc,
        CancellationToken ct)
    {
        var job = await context.IngestionJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null)
        {
            return;
        }

        job.Attempts += 1;
        job.LastError = error;
        job.ClaimedAt = null;
        job.UpdatedAt = nowUtc;

        if (job.Attempts >= maxAttempts)
        {
            job.Status = IngestionJobStatus.Failed;
            job.CompletedAt = nowUtc;
        }
        else
        {
            job.Status = IngestionJobStatus.Pending;
            job.NextAttemptAt = nextAttemptAtUtc;
            job.CompletedAt = null;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<IngestionJob>> ListAsync(IngestionJobStatus? status, int limit, CancellationToken ct)
    {
        var query = context.IngestionJobs.AsNoTracking();
        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        return await query
            .OrderByDescending(j => j.UpdatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<bool> RetryAsync(Guid jobId, DateTime nextAttemptAtUtc, CancellationToken ct)
    {
        var job = await context.IngestionJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null || job.Status != IngestionJobStatus.Failed)
        {
            return false;
        }

        job.Status = IngestionJobStatus.Pending;
        job.NextAttemptAt = nextAttemptAtUtc;
        job.LastError = null;
        job.ClaimedAt = null;
        job.CompletedAt = null;
        job.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return true;
    }
}
