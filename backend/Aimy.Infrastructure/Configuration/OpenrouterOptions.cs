using System.ComponentModel.DataAnnotations;

namespace Aimy.Infrastructure.Configuration;

public sealed class OpenrouterOptions
{
    public const string SectionName = "Openrouter";

    [Required]
    public required string Endpoint { get; set; }

    [Required]
    public required string ApiKey { get; set; }
}
