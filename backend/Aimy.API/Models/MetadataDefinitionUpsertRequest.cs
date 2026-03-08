using Aimy.Core.Domain.Entities;

namespace Aimy.API.Models;

public class MetadataDefinitionUpsertRequest
{
    public required string Key { get; set; }
    public required string Label { get; set; }
    public string ValueType { get; set; } = "string";
    public bool Filterable { get; set; }
    public bool AllowFreeText { get; set; }
    public bool Required { get; set; }
    public MetadataNormalizationPolicy Policy { get; set; } = MetadataNormalizationPolicy.Permissive;
    public bool IsActive { get; set; } = true;
}
