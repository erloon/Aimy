namespace Aimy.API.Models;

/// <summary>
/// Standard error response model
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error message describing what went wrong
    /// </summary>
    /// <example>File size must not exceed 50MB</example>
    public required string Error { get; set; }
}
