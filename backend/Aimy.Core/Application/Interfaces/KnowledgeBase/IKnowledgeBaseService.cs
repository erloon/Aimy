using Aimy.Core.Application.DTOs.KnowledgeBase;

namespace Aimy.Core.Application.Interfaces.KnowledgeBase;

public interface IKnowledgeBaseService
{
    Task<KnowledgeBaseResponse> GetOrCreateAsync(CancellationToken ct);
}
