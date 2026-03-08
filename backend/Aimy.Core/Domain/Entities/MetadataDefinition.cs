namespace Aimy.Core.Domain.Entities;

public class MetadataDefinition
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Label { get; set; }
    public required string ValueType { get; set; }
    public bool Filterable { get; set; }
    public bool AllowFreeText { get; set; }
    public bool Required { get; set; }
    public MetadataNormalizationPolicy Policy { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<MetadataValueOption> ValueOptions { get; set; } = new List<MetadataValueOption>();

    public MetadataDefinition()
    {
        Id = Guid.NewGuid();
        ValueType = "string";
        Policy = MetadataNormalizationPolicy.Permissive;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
