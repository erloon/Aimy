namespace Aimy.Core.Application.DTOs.KnowledgeBase;

/// <summary>
/// Request model for creating a new folder
/// </summary>
public class CreateFolderRequest
{
    /// <summary>
    /// Name of the folder
    /// </summary>
    /// <example>Project Documents</example>
    public required string Name { get; set; }

    /// <summary>
    /// Parent folder ID (null for root folders)
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? ParentFolderId { get; set; }
}
