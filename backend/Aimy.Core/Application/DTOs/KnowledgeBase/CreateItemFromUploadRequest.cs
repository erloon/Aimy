namespace Aimy.Core.Application.DTOs.KnowledgeBase;

/// <summary>
/// Request model for creating an item from an existing upload
/// </summary>
public class CreateItemFromUploadRequest
{
    /// <summary>
    /// Folder to create the item in
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid FolderId { get; set; }

    /// <summary>
    /// Upload ID to link to this item
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid UploadId { get; set; }

    /// <summary>
    /// Title of the item (defaults to filename if not provided)
    /// </summary>
    /// <example>Important Document</example>
    public string? Title { get; set; }

    /// <summary>
    /// JSON array of tags
    /// </summary>
    /// <example>["document", "important"]</example>
    public string? Tags { get; set; }
}
