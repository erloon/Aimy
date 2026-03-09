namespace Aimy.Core.Application.DTOs.KnowledgeBase;

/// <summary>
/// Request model for uploading a file and creating a knowledge item.
/// </summary>
public class UploadToFolderRequest
{
    /// <summary>
    /// Folder to create the item in.
    /// </summary>
    public Guid FolderId { get; set; }

    /// <summary>
    /// Optional title for the item (defaults to file name).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Optional JSON metadata payload.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Original file name.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Optional content type.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// File content stream.
    /// </summary>
    public required Stream FileStream { get; set; }
}
