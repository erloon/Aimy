namespace Aimy.Core.Application.DTOs.KnowledgeBase;

using Aimy.Core.Domain.Entities;

/// <summary>
/// Request model for searching knowledge items
/// </summary>
public class ItemSearchRequest
{
    /// <summary>
    /// Search query for title and content
    /// </summary>
    /// <example>meeting</example>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by metadata (JSON object)
    /// </summary>
    /// <example>{"category":"meeting"}</example>
    public string? Metadata { get; set; }

    /// <summary>
    /// Filter by item type
    /// </summary>
    /// <example>Note</example>
    public KnowledgeItemType? Type { get; set; }

    /// <summary>
    /// Filter by folder (optional, searches all folders if not specified)
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? FolderId { get; set; }

    /// <summary>
    /// Include items from all descendant folders when FolderId is specified
    /// </summary>
    /// <example>true</example>
    public bool IncludeSubFolders { get; set; } = false;

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    /// <example>1</example>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    /// <example>10</example>
    public int PageSize { get; set; } = 10;
}
