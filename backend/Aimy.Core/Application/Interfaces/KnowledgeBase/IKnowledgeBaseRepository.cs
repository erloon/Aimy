namespace Aimy.Core.Application.Interfaces.KnowledgeBase;

public interface IKnowledgeBaseRepository
{
    Task<Domain.Entities.KnowledgeBase> GetOrCreateForUserAsync(Guid userId, CancellationToken ct);
    Task<Domain.Entities.KnowledgeBase?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Domain.Entities.KnowledgeBase?> GetByUserIdAsync(Guid userId, CancellationToken ct);
}
