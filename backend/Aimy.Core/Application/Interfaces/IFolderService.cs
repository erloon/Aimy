namespace Aimy.Core.Application.Interfaces;

using Aimy.Core.Application.DTOs.KnowledgeBase;

public interface IFolderService
{
    Task<FolderResponse> CreateAsync(CreateFolderRequest request, CancellationToken ct);
    Task<FolderResponse> UpdateAsync(Guid id, UpdateFolderRequest request, CancellationToken ct);
    Task<FolderResponse> MoveAsync(Guid id, MoveFolderRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<FolderTreeResponse> GetTreeAsync(CancellationToken ct);
}
