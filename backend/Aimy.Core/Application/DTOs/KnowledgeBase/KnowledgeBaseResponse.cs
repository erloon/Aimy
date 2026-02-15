namespace Aimy.Core.Application.DTOs.KnowledgeBase;

/// <summary>
/// Response model for knowledge base information
/// </summary>
public class KnowledgeBaseResponse
{
    /// <summary>
    /// Unique identifier for the knowledge base
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>
    /// User ID that owns this knowledge base
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid UserId { get; set; }

    /// <summary>
    /// Timestamp when the knowledge base was created (UTC)
    /// </summary>
    /// <example>2024-02-14T20:00:00Z</example>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the knowledge base was last updated (UTC)
    /// </summary>
    /// <example>2024-02-14T20:00:00Z</example>
    public DateTime UpdatedAt { get; set; }
}
