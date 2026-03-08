using Aimy.Core.Domain.Entities;

namespace Aimy.Core.Application.Interfaces.Metadata;

public interface IMetadataCatalogRepository
{
    Task<IReadOnlyList<MetadataDefinition>> GetDefinitionsAsync(CancellationToken ct);
    Task<MetadataDefinition?> GetDefinitionByKeyAsync(string key, CancellationToken ct);
    Task<IReadOnlyList<MetadataValueOption>> GetValueOptionsAsync(string key, string? prefix, CancellationToken ct);
    Task<IReadOnlyList<MetadataValueOption>> GetAllValueOptionsAsync(string key, CancellationToken ct);
    Task<MetadataDefinition> UpsertDefinitionAsync(MetadataDefinition definition, CancellationToken ct);
    Task<MetadataValueOption> UpsertValueOptionAsync(string key, MetadataValueOption option, CancellationToken ct);
}
