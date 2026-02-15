namespace Aimy.Core.Application.DTOs.KnowledgeBase;

/// <summary>
/// Request model for updating a folder
/// </summary>
public class UpdateFolderRequest
{
    /// <summary>
    /// New name for the folder
    /// </summary>
    /// <example>Renamed Folder</example>
    public required string Name { get; set; }
}
