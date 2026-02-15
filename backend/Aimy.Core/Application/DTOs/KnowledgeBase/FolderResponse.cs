namespace Aimy.Core.Application.DTOs.KnowledgeBase;

/// <summary>
/// Response model for folder information
/// </summary>
public class FolderResponse
{
    /// <summary>
    /// Unique identifier for the folder
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Knowledge base this folder belongs to
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid KnowledgeBaseId { get; set; }

    /// <summary>
    /// Parent folder ID (null for root folders)
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? ParentFolderId { get; set; }

    /// <summary>
    /// Name of the folder
    /// </summary>
    /// <example>Project Documents</example>
    public required string Name { get; set; }

    /// <summary>
    /// Timestamp when the folder was created (UTC)
    /// </summary>
    /// <example>2024-02-14T20:00:00Z</example>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the folder was last updated (UTC)
    /// </summary>
    /// <example>2024-02-14T20:00:00Z</example>
    public DateTime UpdatedAt { get; set; }
}
