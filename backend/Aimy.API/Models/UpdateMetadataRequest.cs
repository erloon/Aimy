using System.ComponentModel.DataAnnotations;

namespace Aimy.API.Models;

/// <summary>
/// Request model for updating file metadata
/// </summary>
public class UpdateMetadataRequest
{
    /// <summary>
    /// JSON string containing key-value pairs for file metadata
    /// </summary>
    /// <example>{"category": "documents", "tags": ["important", "2024"]}</example>
    [MaxLength(2000)]
    public string? Metadata { get; set; }
}
