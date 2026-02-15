namespace Aimy.Core.Application.DTOs.KnowledgeBase;

/// <summary>
/// Request model for updating a knowledge item
/// </summary>
public class UpdateItemRequest
{
    /// <summary>
    /// New title for the item
    /// </summary>
    /// <example>Updated Title</example>
    public string? Title { get; set; }

    /// <summary>
    /// New markdown content (for notes)
    /// </summary>
    /// <example># Updated Content</example>
    public string? Content { get; set; }

    /// <summary>
    /// New JSON array of tags
    /// </summary>
    /// <example>["updated", "tags"]</example>
    public string? Tags { get; set; }

    /// <summary>
    /// New folder to move the item to
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? FolderId { get; set; }
}
