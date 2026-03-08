namespace Aimy.Core.Application.DTOs.KnowledgeBase;

public class FolderContentSummary
{
    public int ItemCount { get; set; }
    public int SubfolderCount { get; set; }
    public bool HasContent => ItemCount > 0 || SubfolderCount > 0;
}
