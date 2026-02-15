namespace Aimy.Core.Application.Interfaces;

using Aimy.Core.Domain.Entities;

public interface IKnowledgeBaseRepository
{
    Task<KnowledgeBase> GetOrCreateForUserAsync(Guid userId, CancellationToken ct);
    Task<KnowledgeBase?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<KnowledgeBase?> GetByUserIdAsync(Guid userId, CancellationToken ct);
}
