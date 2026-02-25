using Aimy.Core.Application.DTOs.KnowledgeBase;

namespace Aimy.Core.Application.Interfaces.KnowledgeBase;

public interface IFolderService
{
    Task<FolderResponse> CreateAsync(CreateFolderRequest request, CancellationToken ct);
    Task<FolderResponse> UpdateAsync(Guid id, UpdateFolderRequest request, CancellationToken ct);
    Task<FolderResponse> MoveAsync(Guid id, MoveFolderRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, bool force, CancellationToken ct);
    Task<FolderContentSummary> GetContentSummaryAsync(Guid id, CancellationToken ct);
    Task<FolderTreeResponse> GetTreeAsync(CancellationToken ct);
}
