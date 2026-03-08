namespace Aimy.API.Models;

public class MetadataKeysResponse
{
    public required IReadOnlyList<MetadataKeyResponseItem> Items { get; set; }
}

public class MetadataKeyResponseItem
{
    public required string Key { get; set; }
    public required string Label { get; set; }
    public required string Type { get; set; }
    public bool Filterable { get; set; }
    public bool AllowFreeText { get; set; }
    public bool Required { get; set; }
    public required string Policy { get; set; }
}

public class MetadataValuesResponse
{
    public required string Key { get; set; }
    public required IReadOnlyList<MetadataValueSuggestionResponseItem> Items { get; set; }
}

public class MetadataValueSuggestionResponseItem
{
    public required string Value { get; set; }
    public required string Label { get; set; }
    public string[] Aliases { get; set; } = [];
    public required string MatchType { get; set; }
}

public class MetadataNormalizeResponse
{
    public string? Metadata { get; set; }
    public bool HasChanges { get; set; }
    public required IReadOnlyList<MetadataNormalizeWarningResponse> Warnings { get; set; }
}

public class MetadataNormalizeWarningResponse
{
    public required string Key { get; set; }
    public required string Message { get; set; }
    public string? InputValue { get; set; }
    public string? ResolvedValue { get; set; }
    public required string MatchType { get; set; }
}

public class MetadataDefinitionResponse
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Label { get; set; }
    public required string Type { get; set; }
    public bool Filterable { get; set; }
    public bool AllowFreeText { get; set; }
    public bool Required { get; set; }
    public required string Policy { get; set; }
    public bool IsActive { get; set; }
}

public class MetadataValueOptionResponse
{
    public Guid Id { get; set; }
    public required string Value { get; set; }
    public required string Label { get; set; }
    public string[] Aliases { get; set; } = [];
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
