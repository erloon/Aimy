using Aimy.Core.Domain.Entities;

namespace Aimy.Core.Application.Interfaces.KnowledgeBase;

public interface IFolderRepository
{
    Task<Folder> AddAsync(Folder folder, CancellationToken ct);
    Task<Folder?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<Folder>> GetByKnowledgeBaseIdAsync(Guid knowledgeBaseId, CancellationToken ct);
    Task<IEnumerable<Folder>> GetChildrenAsync(Guid parentFolderId, CancellationToken ct);
    Task<IReadOnlyList<Folder>> GetFolderTreeAsync(Guid knowledgeBaseId, CancellationToken ct);
    Task UpdateAsync(Folder folder, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<bool> HasChildrenAsync(Guid folderId, CancellationToken ct);
    Task<bool> HasItemsAsync(Guid folderId, CancellationToken ct);
}
