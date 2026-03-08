namespace Aimy.Core.Domain.Entities;

public enum MetadataNormalizationPolicy
{
    Strict = 1,
    Permissive = 2
}

public enum MetadataMatchType
{
    ExactCanonical = 1,
    Alias = 2,
    Fuzzy = 3,
    Custom = 4,
    Rejected = 5
}
