namespace Aimy.Core.Domain.Entities;

public class MetadataValueOption
{
    public Guid Id { get; set; }
    public Guid MetadataDefinitionId { get; set; }
    public required string CanonicalValue { get; set; }
    public required string DisplayLabel { get; set; }
    public string[] Aliases { get; set; } = [];
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public MetadataDefinition? Definition { get; set; }

    public MetadataValueOption()
    {
        Id = Guid.NewGuid();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
