namespace Aimy.Core.Application.Interfaces;

using Aimy.Core.Application.DTOs;
using Aimy.Core.Domain.Entities;

public interface IKnowledgeItemRepository
{
    Task<KnowledgeItem> AddAsync(KnowledgeItem item, CancellationToken ct);
    Task<KnowledgeItem?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<KnowledgeItem>> GetByFolderIdAsync(Guid folderId, CancellationToken ct);
    Task<PagedResult<KnowledgeItem>> SearchAsync(
        Guid knowledgeBaseId,
        Guid? folderId,
        IReadOnlyCollection<Guid>? folderIds,
        string? search,
        string? tags,
        KnowledgeItemType? type,
        int page,
        int pageSize,
        CancellationToken ct);
    Task UpdateAsync(KnowledgeItem item, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
