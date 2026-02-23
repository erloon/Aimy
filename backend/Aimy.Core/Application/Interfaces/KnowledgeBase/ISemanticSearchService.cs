using Aimy.Core.Application.DTOs.KnowledgeBase;

namespace Aimy.Core.Application.Interfaces.KnowledgeBase;

public interface ISemanticSearchService
{
    Task<IReadOnlyList<SemanticSearchResultResponse>> SearchAsync(string query, CancellationToken ct);
}
