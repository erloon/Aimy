using Aimy.Core.Domain.Entities;

namespace Aimy.API.Models;

public class MetadataNormalizeRequest
{
    public string? Metadata { get; set; }
    public MetadataNormalizationPolicy DefaultPolicy { get; set; } = MetadataNormalizationPolicy.Permissive;
}
