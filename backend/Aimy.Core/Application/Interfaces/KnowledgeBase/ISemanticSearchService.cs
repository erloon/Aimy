using Aimy.Core.Application.DTOs;
using Aimy.Core.Application.DTOs.KnowledgeBase;

namespace Aimy.Core.Application.Interfaces.KnowledgeBase;

public interface ISemanticSearchService
{
    Task<PagedResult<SemanticSearchResultResponse>> SearchAsync(string query, int page, int pageSize, CancellationToken ct);
}
