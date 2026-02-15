namespace Aimy.Infrastructure.Repositories;

using Aimy.Core.Application.Interfaces;
using Aimy.Core.Domain.Entities;
using Aimy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
}
