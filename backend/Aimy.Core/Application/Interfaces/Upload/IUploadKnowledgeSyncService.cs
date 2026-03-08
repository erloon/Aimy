using Aimy.Core.Application.DTOs.Metadata;
using Aimy.Core.Domain.Entities;

namespace Aimy.Core.Application.Interfaces.Upload;

public interface IUploadKnowledgeSyncService
{
    string? NormalizeMetadataPayload(string? metadata);
    Task<MetadataNormalizationResult> NormalizeMetadataAsync(string? metadata, MetadataNormalizationPolicy defaultPolicy, CancellationToken ct);
    Task<IReadOnlyList<MetadataDefinition>> GetMetadataDefinitionsAsync(CancellationToken ct);
    Task<MetadataValueSuggestions> GetMetadataValueSuggestionsAsync(string key, string? prefix, CancellationToken ct);
    Task<MetadataDefinition> UpsertDefinitionAsync(MetadataDefinition definition, CancellationToken ct);
    Task<MetadataValueOption> UpsertValueOptionAsync(string key, MetadataValueOption option, CancellationToken ct);
    Task SyncMetadataAsync(Aimy.Core.Domain.Entities.Upload upload, string? metadata, CancellationToken ct);
    Task EnqueueIngestionAsync(Guid uploadId, CancellationToken ct);
    Task ReingestAsync(Guid uploadId, CancellationToken ct);
}
