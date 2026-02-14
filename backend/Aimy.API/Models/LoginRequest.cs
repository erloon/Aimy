using System.ComponentModel.DataAnnotations;

namespace Aimy.API.Models;

/// <summary>
/// Request model for user authentication
/// </summary>
public record LoginRequest
{
    /// <summary>
    /// Username for authentication
    /// </summary>
    /// <example>admin</example>
    [Required]
    public required string Username { get; init; }

    /// <summary>
    /// Password for authentication
    /// </summary>
    /// <example>password123</example>
    [Required]
    public required string Password { get; init; }
}
