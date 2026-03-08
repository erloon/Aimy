namespace Aimy.Core.Application.DTOs.KnowledgeBase;

public class SemanticSearchResultResponse
{
    public required ItemResponse ItemResponse { get; set; }
    public required double Score { get; set; }
}