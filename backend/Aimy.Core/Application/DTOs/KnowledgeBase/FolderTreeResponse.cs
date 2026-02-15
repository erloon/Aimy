namespace Aimy.Core.Application.DTOs.KnowledgeBase;

/// <summary>
/// Response model for folder tree structure
/// </summary>
public class FolderTreeResponse
{
    /// <summary>
    /// List of root-level folder nodes
    /// </summary>
    public required IReadOnlyList<FolderTreeNode> RootFolders { get; set; }
}

/// <summary>
/// Node in the folder tree
/// </summary>
public class FolderTreeNode
{
    /// <summary>
    /// Unique identifier for the folder
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the folder
    /// </summary>
    /// <example>Project Documents</example>
    public required string Name { get; set; }

    /// <summary>
    /// Child folders
    /// </summary>
    public required IReadOnlyList<FolderTreeNode> Children { get; set; }
}
