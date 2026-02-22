using Aimy.Core.Application.DTOs;
using Aimy.Core.Domain.Entities;

namespace Aimy.Core.Application.Interfaces.KnowledgeBase;

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
        string? metadata,
        KnowledgeItemType? type,
        int page,
        int pageSize,
        CancellationToken ct);
    Task<bool> ExistsBySourceUploadIdAsync(Guid sourceUploadId, CancellationToken ct);
    Task<IReadOnlyCollection<KnowledgeItem>> GetBySourceUploadIdAsync(Guid sourceUploadId, CancellationToken ct);
    Task UpdateAsync(KnowledgeItem item, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
