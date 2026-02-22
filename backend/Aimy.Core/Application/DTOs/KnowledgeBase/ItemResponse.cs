namespace Aimy.Core.Application.DTOs.KnowledgeBase;

using Aimy.Core.Domain.Entities;

/// <summary>
/// Response model for knowledge item information
/// </summary>
public class ItemResponse
{
    /// <summary>
    /// Unique identifier for the item
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Folder this item belongs to
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid FolderId { get; set; }

    /// <summary>
    /// Folder name for display purposes
    /// </summary>
    /// <example>Frameworks</example>
    public string? FolderName { get; set; }

    /// <summary>
    /// Title of the item
    /// </summary>
    /// <example>Meeting Notes</example>
    public required string Title { get; set; }

    /// <summary>
    /// Type of the item (File or Note)
    /// </summary>
    /// <example>Note</example>
    public KnowledgeItemType ItemType { get; set; }

    /// <summary>
    /// Markdown content for notes
    /// </summary>
    /// <example># Meeting Notes\n- Item 1\n- Item 2</example>
    public string? Content { get; set; }

    /// <summary>
    /// Metadata JSON object
    /// </summary>
    /// <example>{"category":"meeting","tags":["project-x"]}</example>
    public string? Metadata { get; set; }

    /// <summary>
    /// Source upload ID for file items
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? SourceUploadId { get; set; }

    /// <summary>
    /// Source upload filename (if linked)
    /// </summary>
    /// <example>My Note.md</example>
    public string? SourceUploadFileName { get; set; }

    /// <summary>
    /// Source upload metadata (if linked)
    /// </summary>
    /// <example>{"category":"docs"}</example>
    public string? SourceUploadMetadata { get; set; }

    /// <summary>
    /// Timestamp when the item was created (UTC)
    /// </summary>
    /// <example>2024-02-14T20:00:00Z</example>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the item was last updated (UTC)
    /// </summary>
    /// <example>2024-02-14T20:00:00Z</example>
    public DateTime UpdatedAt { get; set; }
}
