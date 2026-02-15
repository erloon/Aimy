namespace Aimy.Core.Domain.Entities;

/// <summary>
/// Item within the knowledge base (File or Note).
/// Invariant: Item always belongs to a Folder (source of truth).
/// Invariant: Deleting a Note item does NOT delete the underlying Upload (unlink only).
/// </summary>
public class KnowledgeItem
{
    public Guid Id { get; set; }
    public Guid FolderId { get; set; }
    public required string Title { get; set; }
    public KnowledgeItemType ItemType { get; set; }
    public string? Content { get; set; }
    public string? Tags { get; set; }
    public Guid? SourceUploadId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Folder? Folder { get; set; }
    public Upload? SourceUpload { get; set; }

    public KnowledgeItem()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum KnowledgeItemType
{
    File = 1,
    Note = 2
}
