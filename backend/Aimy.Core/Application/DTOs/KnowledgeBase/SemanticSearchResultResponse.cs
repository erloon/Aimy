namespace Aimy.Core.Application.DTOs.KnowledgeBase;

public class SemanticSearchResultResponse
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string ItemType { get; set; }
    public string? Content { get; set; }
    public string? Metadata { get; set; }
    public string? FolderName { get; set; }
    public Guid? SourceUploadId { get; set; }
    public string? SourceUploadFileName { get; set; }
    public required double Score { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
}
