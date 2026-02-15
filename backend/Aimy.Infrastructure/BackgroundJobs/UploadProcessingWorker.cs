using Aimy.Core.Application.Interfaces.Upload;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aimy.Infrastructure.BackgroundJobs;

public class UploadProcessingWorker(IUploadQueueReader reader, ILogger<UploadProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var upload in reader.ReadAllAsync(stoppingToken))
        {
            logger.LogWarning($"File: {upload.UploadId}");
        }
    }
}