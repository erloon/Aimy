namespace Aimy.Core.Domain.Entities;

/// <summary>
/// Root container for a user's knowledge base.
/// Invariant: One KnowledgeBase per user (enforced by unique constraint in Infrastructure).
/// </summary>
public class KnowledgeBase
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Folder> Folders { get; set; } = [];

    public KnowledgeBase()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
