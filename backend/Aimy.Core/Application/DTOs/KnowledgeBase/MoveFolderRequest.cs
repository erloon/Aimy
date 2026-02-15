namespace Aimy.Core.Application.DTOs.KnowledgeBase;

/// <summary>
/// Request model for moving a folder to a new parent
/// </summary>
public class MoveFolderRequest
{
    /// <summary>
    /// New parent folder ID (null to move to root)
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? NewParentFolderId { get; set; }
}
