using Aimy.Core.Application.Interfaces.Ingestion;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aimy.Infrastructure.BackgroundJobs;

public class UploadProcessingWorker(
    ILogger<UploadProcessingWorker> logger,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var claimScope = serviceScopeFactory.CreateScope();
            var ingestionJobService = claimScope.ServiceProvider.GetRequiredService<IIngestionJobService>();
            var claimedJob = await ingestionJobService.ClaimNextAsync(stoppingToken);
            if (claimedJob is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            try
            {
                logger.LogInformation("Processing upload {UploadId} (job {JobId})", claimedJob.UploadId, claimedJob.JobId);
                using var processingScope = serviceScopeFactory.CreateScope();
                var dataIngestionService = processingScope.ServiceProvider.GetRequiredService<IDataIngestionService>();
                var processingJobService = processingScope.ServiceProvider.GetRequiredService<IIngestionJobService>();
                await dataIngestionService.IngestDataAsync(claimedJob.UploadId, stoppingToken);
                await processingJobService.MarkCompletedAsync(claimedJob.JobId, claimedJob.UploadId, stoppingToken);
                logger.LogInformation("Completed upload processing for {UploadId} (job {JobId})", claimedJob.UploadId, claimedJob.JobId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                using var failureScope = serviceScopeFactory.CreateScope();
                var failureJobService = failureScope.ServiceProvider.GetRequiredService<IIngestionJobService>();
                await failureJobService.MarkFailedAsync(claimedJob.JobId, claimedJob.UploadId, ex, stoppingToken);
                logger.LogError(ex, "Upload processing failed for {UploadId} (job {JobId})", claimedJob.UploadId, claimedJob.JobId);
            }
        }
    }
}
