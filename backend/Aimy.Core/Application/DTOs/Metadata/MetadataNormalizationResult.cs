using Aimy.Core.Domain.Entities;

namespace Aimy.Core.Application.DTOs.Metadata;

public class MetadataNormalizationResult
{
    public string? NormalizedMetadata { get; set; }
    public required IReadOnlyList<MetadataNormalizationWarning> Warnings { get; set; }
    public bool HasChanges { get; set; }
}

public class MetadataNormalizationWarning
{
    public required string Key { get; set; }
    public required string Message { get; set; }
    public string? InputValue { get; set; }
    public string? ResolvedValue { get; set; }
    public MetadataMatchType MatchType { get; set; }
}

public class MetadataValueSuggestions
{
    public required string Key { get; set; }
    public required IReadOnlyList<MetadataValueSuggestionItem> Items { get; set; }
}

public class MetadataValueSuggestionItem
{
    public required string Value { get; set; }
    public required string Label { get; set; }
    public string[] Aliases { get; set; } = [];
    public required string MatchType { get; set; }
}
