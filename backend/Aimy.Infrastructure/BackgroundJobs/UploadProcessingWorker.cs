using Aimy.Core.Application.Interfaces.Ingestion;
using Aimy.Core.Application.Interfaces.Upload;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aimy.Infrastructure.BackgroundJobs;

public class UploadProcessingWorker(
    IUploadQueueReader reader,
    ILogger<UploadProcessingWorker> logger,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var upload in reader.ReadAllAsync(stoppingToken))
        {
            logger.LogWarning($"File: {upload.UploadId}");
            using var scope = serviceScopeFactory.CreateScope();
            var dataIngestionService = scope.ServiceProvider.GetRequiredService<IDataIngestionService>();
            await dataIngestionService.IngestDataAsync(upload.UploadId, stoppingToken);
        }
    }
}
