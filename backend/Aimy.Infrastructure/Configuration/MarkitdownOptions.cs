using System.ComponentModel.DataAnnotations;

namespace Aimy.Infrastructure.Configuration;

public sealed class MarkitdownOptions
{
    public const string SectionName = "Markitdown";

    [Required]
    public required string McpUrl { get; set; }
}
