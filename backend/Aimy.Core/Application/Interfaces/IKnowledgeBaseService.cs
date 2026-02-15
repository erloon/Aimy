namespace Aimy.Core.Application.Interfaces;

using Aimy.Core.Application.DTOs.KnowledgeBase;

public interface IKnowledgeBaseService
{
    Task<KnowledgeBaseResponse> GetOrCreateAsync(CancellationToken ct);
}
