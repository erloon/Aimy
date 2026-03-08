namespace Aimy.API.Models;

public class MetadataValueOptionUpsertRequest
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required string Label { get; set; }
    public string[] Aliases { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
