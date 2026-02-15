using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Application.Interfaces.Upload;

namespace Aimy.Infrastructure.Repositories;

using Aimy.Core.Application.DTOs;
using Aimy.Core.Application.Interfaces;
using Aimy.Core.Domain.Entities;
using Aimy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class KnowledgeItemRepository(ApplicationDbContext context) : IKnowledgeItemRepository
{
    public async Task<KnowledgeItem> AddAsync(KnowledgeItem item, CancellationToken ct)
    {
        context.KnowledgeItems.Add(item);
        await context.SaveChangesAsync(ct);
        return item;
    }

    public async Task<KnowledgeItem?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await context.KnowledgeItems
            .Include(ki => ki.Folder!)
            .ThenInclude(f => f!.KnowledgeBase)
            .Include(ki => ki.SourceUpload)
            .FirstOrDefaultAsync(ki => ki.Id == id, ct);
    }

    public async Task<IEnumerable<KnowledgeItem>> GetByFolderIdAsync(Guid folderId, CancellationToken ct)
    {
        return await context.KnowledgeItems
            .Include(ki => ki.SourceUpload)
            .Where(ki => ki.FolderId == folderId)
            .OrderByDescending(ki => ki.UpdatedAt)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<KnowledgeItem>> SearchAsync(
        Guid knowledgeBaseId,
        Guid? folderId,
        IReadOnlyCollection<Guid>? folderIds,
        string? search,
        string? tags,
        KnowledgeItemType? type,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.KnowledgeItems
            .Include(ki => ki.Folder!)
            .ThenInclude(f => f.KnowledgeBase)
            .Include(ki => ki.SourceUpload)
            .Where(ki => ki.Folder!.KnowledgeBaseId == knowledgeBaseId);

        if (folderIds is { Count: > 0 })
        {
            query = query.Where(ki => folderIds.Contains(ki.FolderId));
        }
        else if (folderId.HasValue)
        {
            query = query.Where(ki => ki.FolderId == folderId.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(ki => 
                ki.Title.Contains(search) || 
                (ki.Content != null && ki.Content.Contains(search)));
        }
        
        if (type.HasValue)
        {
            query = query.Where(ki => ki.ItemType == type.Value);
        }
        
        // For MVP, tags is JSON string - skip complex filtering
        // if (!string.IsNullOrWhiteSpace(tags))
        // {
        //     query = query.Where(ki => ki.Tags != null && ki.Tags.Contains(tags));
        // }
        
        var totalCount = await query.CountAsync(ct);
        
        var items = await query
            .OrderByDescending(ki => ki.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        
        return new PagedResult<KnowledgeItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task UpdateAsync(KnowledgeItem item, CancellationToken ct)
    {
        context.KnowledgeItems.Update(item);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var item = await context.KnowledgeItems.FindAsync([id], ct);
        if (item != null)
        {
            context.KnowledgeItems.Remove(item);
            await context.SaveChangesAsync(ct);
        }
    }
}
