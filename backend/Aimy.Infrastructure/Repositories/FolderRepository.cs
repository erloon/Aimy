using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Domain.Entities;
using Aimy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aimy.Infrastructure.Repositories;

public class FolderRepository(ApplicationDbContext context) : IFolderRepository
{
    public async Task<Folder> AddAsync(Folder folder, CancellationToken ct)
    {
        context.Folders.Add(folder);
        await context.SaveChangesAsync(ct);
        return folder;
    }

    public async Task<Folder?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Folders
            .FirstOrDefaultAsync(f => f.Id == id, ct);
    }

    public async Task<IEnumerable<Folder>> GetByKnowledgeBaseIdAsync(Guid knowledgeBaseId, CancellationToken ct)
    {
        return await context.Folders
            .Where(f => f.KnowledgeBaseId == knowledgeBaseId)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Folder>> GetChildrenAsync(Guid parentFolderId, CancellationToken ct)
    {
        return await context.Folders
            .Where(f => f.ParentFolderId == parentFolderId)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Folder>> GetFolderTreeAsync(Guid knowledgeBaseId, CancellationToken ct)
    {
        return await context.Folders
            .Where(f => f.KnowledgeBaseId == knowledgeBaseId)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(Folder folder, CancellationToken ct)
    {
        context.Folders.Update(folder);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var folder = await context.Folders.FindAsync([id], ct);
        if (folder != null)
        {
            context.Folders.Remove(folder);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task<(int ItemCount, int SubfolderCount)> GetContentSummaryAsync(Guid folderId, CancellationToken ct)
    {
        var descendantFolderIds = await GetDescendantFolderIdsAsync(folderId, ct);
        var allFolderIds = new List<Guid>(descendantFolderIds.Count + 1) { folderId };
        allFolderIds.AddRange(descendantFolderIds);

        var itemCount = await context.KnowledgeItems
            .CountAsync(ki => allFolderIds.Contains(ki.FolderId), ct);

        return (itemCount, descendantFolderIds.Count);
    }

    public async Task DeleteWithContentsAsync(Guid folderId, CancellationToken ct)
    {
        var descendantFolderIds = await GetDescendantFolderIdsAsync(folderId, ct);
        var allFolderIds = new List<Guid>(descendantFolderIds.Count + 1) { folderId };
        allFolderIds.AddRange(descendantFolderIds);

        var itemsToDelete = await context.KnowledgeItems
            .Where(ki => allFolderIds.Contains(ki.FolderId))
            .ToListAsync(ct);

        if (itemsToDelete.Count > 0)
        {
            context.KnowledgeItems.RemoveRange(itemsToDelete);
        }

        var foldersById = await context.Folders
            .Where(f => allFolderIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, ct);

        foreach (var descendantId in descendantFolderIds.AsEnumerable().Reverse())
        {
            if (foldersById.TryGetValue(descendantId, out var descendantFolder))
            {
                context.Folders.Remove(descendantFolder);
            }
        }

        if (foldersById.TryGetValue(folderId, out var folder))
        {
            context.Folders.Remove(folder);
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> HasChildrenAsync(Guid folderId, CancellationToken ct)
    {
        return await context.Folders
            .AnyAsync(f => f.ParentFolderId == folderId, ct);
    }

    public async Task<bool> HasItemsAsync(Guid folderId, CancellationToken ct)
    {
        return await context.KnowledgeItems
            .AnyAsync(ki => ki.FolderId == folderId, ct);
    }

    private async Task<List<Guid>> GetDescendantFolderIdsAsync(Guid folderId, CancellationToken ct)
    {
        var descendants = new List<Guid>();
        await CollectDescendantFolderIdsAsync(folderId, descendants, ct);
        return descendants;
    }

    private async Task CollectDescendantFolderIdsAsync(Guid folderId, List<Guid> descendants, CancellationToken ct)
    {
        var childFolderIds = await context.Folders
            .Where(f => f.ParentFolderId == folderId)
            .Select(f => f.Id)
            .ToListAsync(ct);

        foreach (var childFolderId in childFolderIds)
        {
            descendants.Add(childFolderId);
            await CollectDescendantFolderIdsAsync(childFolderId, descendants, ct);
        }
    }
}
