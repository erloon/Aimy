using System.Text.Json;
using Aimy.Core.Application.Interfaces.Ingestion;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Application.Interfaces.Upload;

namespace Aimy.Core.Application.Services;

public class UploadKnowledgeSyncService(
    IUploadRepository uploadRepository,
    IKnowledgeItemRepository knowledgeItemRepository,
    IDataIngestionService dataIngestionService,
    IIngestionJobService ingestionJobService) : IUploadKnowledgeSyncService
{
    public string? NormalizeMetadataPayload(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadata);
            return document.RootElement.GetRawText();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task SyncMetadataAsync(Aimy.Core.Domain.Entities.Upload upload, string? metadata, CancellationToken ct)
    {
        var canonicalMetadata = NormalizeMetadataPayload(metadata);

        upload.Metadata = canonicalMetadata;
        await uploadRepository.UpdateAsync(upload, ct);
        await dataIngestionService.UpdateMetadataByUploadIdAsync(upload.Id, canonicalMetadata, ct);

        var linkedItems = await knowledgeItemRepository.GetBySourceUploadIdAsync(upload.Id, ct);
        foreach (var linkedItem in linkedItems)
        {
            linkedItem.Metadata = canonicalMetadata;
            linkedItem.UpdatedAt = DateTime.UtcNow;
            await knowledgeItemRepository.UpdateAsync(linkedItem, ct);
        }
    }

    public Task EnqueueIngestionAsync(Guid uploadId, CancellationToken ct)
    {
        return ingestionJobService.EnqueueAsync(uploadId, ct);
    }

    public async Task ReingestAsync(Guid uploadId, CancellationToken ct)
    {
        await dataIngestionService.DeleteByUploadIdAsync(uploadId, ct);
        await ingestionJobService.EnqueueAsync(uploadId, ct);
    }
}
