namespace Aimy.Core.Application.Services;

using Aimy.Core.Application.DTOs.KnowledgeBase;
using Aimy.Core.Application.Interfaces;
using Aimy.Core.Domain.Entities;

public class FolderService(
    IKnowledgeBaseRepository kbRepository,
    IFolderRepository folderRepository,
    ICurrentUserService currentUserService) : IFolderService
{
    public async Task<FolderResponse> CreateAsync(CreateFolderRequest request, CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated");

        var kb = await kbRepository.GetOrCreateForUserAsync(userId, ct);

        // Validate parent folder if specified
        if (request.ParentFolderId.HasValue)
        {
            var parent = await folderRepository.GetByIdAsync(request.ParentFolderId.Value, ct)
                ?? throw new KeyNotFoundException("Parent folder not found");

            if (parent.KnowledgeBaseId != kb.Id)
                throw new UnauthorizedAccessException("Parent folder does not belong to user");
        }

        var folder = new Folder
        {
            KnowledgeBaseId = kb.Id,
            ParentFolderId = request.ParentFolderId,
            Name = request.Name
        };

        var saved = await folderRepository.AddAsync(folder, ct);
        return MapToResponse(saved);
    }

    public async Task<FolderResponse> UpdateAsync(Guid id, UpdateFolderRequest request, CancellationToken ct)
    {
        var (userId, kb) = await EnsureAuthenticatedWithKbAsync(ct);
        var folder = await GetAndValidateOwnershipAsync(id, kb.Id, ct);

        folder.Name = request.Name;
        folder.UpdatedAt = DateTime.UtcNow;

        await folderRepository.UpdateAsync(folder, ct);
        return MapToResponse(folder);
    }

    public async Task<FolderResponse> MoveAsync(Guid id, MoveFolderRequest request, CancellationToken ct)
    {
        var (userId, kb) = await EnsureAuthenticatedWithKbAsync(ct);
        var folder = await GetAndValidateOwnershipAsync(id, kb.Id, ct);

        // Validate new parent if specified
        if (request.NewParentFolderId.HasValue)
        {
            // Can't move to self
            if (request.NewParentFolderId.Value == id)
                throw new InvalidOperationException("Cannot move folder to itself");

            // Can't move to descendant (would create cycle)
            if (await IsDescendantAsync(id, request.NewParentFolderId.Value, ct))
                throw new InvalidOperationException("Cannot move folder to its own descendant");

            var newParent = await folderRepository.GetByIdAsync(request.NewParentFolderId.Value, ct)
                ?? throw new KeyNotFoundException("Target folder not found");

            if (newParent.KnowledgeBaseId != kb.Id)
                throw new UnauthorizedAccessException("Target folder does not belong to user");
        }

        folder.ParentFolderId = request.NewParentFolderId;
        folder.UpdatedAt = DateTime.UtcNow;

        await folderRepository.UpdateAsync(folder, ct);
        return MapToResponse(folder);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var (userId, kb) = await EnsureAuthenticatedWithKbAsync(ct);
        var folder = await GetAndValidateOwnershipAsync(id, kb.Id, ct);

        // Check for children
        if (await folderRepository.HasChildrenAsync(id, ct))
            throw new InvalidOperationException("Cannot delete folder with subfolders");

        // Check for items
        if (await folderRepository.HasItemsAsync(id, ct))
            throw new InvalidOperationException("Cannot delete folder with items");

        await folderRepository.DeleteAsync(id, ct);
    }

    public async Task<FolderTreeResponse> GetTreeAsync(CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated");

        var kb = await kbRepository.GetByUserIdAsync(userId, ct);

        if (kb == null)
            return new FolderTreeResponse { RootFolders = [] };

        var allFolders = await folderRepository.GetFolderTreeAsync(kb.Id, ct);
        var rootFolders = BuildFolderTree(allFolders, null);

        return new FolderTreeResponse { RootFolders = rootFolders };
    }

    // Private helpers
    private async Task<(Guid UserId, KnowledgeBase Kb)> EnsureAuthenticatedWithKbAsync(CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated");
        var kb = await kbRepository.GetOrCreateForUserAsync(userId, ct);
        return (userId, kb);
    }

    private async Task<Folder> GetAndValidateOwnershipAsync(Guid folderId, Guid kbId, CancellationToken ct)
    {
        var folder = await folderRepository.GetByIdAsync(folderId, ct)
            ?? throw new KeyNotFoundException("Folder not found");

        if (folder.KnowledgeBaseId != kbId)
            throw new UnauthorizedAccessException("Folder does not belong to user");

        return folder;
    }

    private async Task<bool> IsDescendantAsync(Guid ancestorId, Guid potentialDescendantId, CancellationToken ct)
    {
        // Check if potentialDescendantId is a descendant of ancestorId
        var currentId = potentialDescendantId;
        var visited = new HashSet<Guid> { currentId };

        while (true)
        {
            var folder = await folderRepository.GetByIdAsync(currentId, ct);
            if (folder == null || folder.ParentFolderId == null)
                return false;

            if (folder.ParentFolderId == ancestorId)
                return true;

            if (visited.Contains(folder.ParentFolderId.Value))
                return false; // Cycle detected, shouldn't happen but safety check

            visited.Add(folder.ParentFolderId.Value);
            currentId = folder.ParentFolderId.Value;
        }
    }

    private static IReadOnlyList<FolderTreeNode> BuildFolderTree(IEnumerable<Folder> allFolders, Guid? parentId)
    {
        return allFolders
            .Where(f => f.ParentFolderId == parentId)
            .Select(f => new FolderTreeNode
            {
                Id = f.Id,
                Name = f.Name,
                Children = BuildFolderTree(allFolders, f.Id)
            })
            .ToList();
    }

    private static FolderResponse MapToResponse(Folder folder)
    {
        return new FolderResponse
        {
            Id = folder.Id,
            KnowledgeBaseId = folder.KnowledgeBaseId,
            ParentFolderId = folder.ParentFolderId,
            Name = folder.Name,
            CreatedAt = folder.CreatedAt,
            UpdatedAt = folder.UpdatedAt
        };
    }
}
