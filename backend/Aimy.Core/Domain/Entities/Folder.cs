namespace Aimy.Core.Domain.Entities;

/// <summary>
/// Hierarchical folder structure for organizing knowledge items.
/// Invariant: Folder always belongs to a KnowledgeBase.
/// Invariant: Cannot delete folder with children or items (enforced in service).
/// Invariant: Cannot move folder to self or descendant (no cycles).
/// </summary>
public class Folder
{
    public Guid Id { get; set; }
    public Guid KnowledgeBaseId { get; set; }
    public Guid? ParentFolderId { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public KnowledgeBase? KnowledgeBase { get; set; }
    public Folder? ParentFolder { get; set; }
    public ICollection<Folder> SubFolders { get; set; } = [];
    public ICollection<KnowledgeItem> Items { get; set; } = [];

    public Folder()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
