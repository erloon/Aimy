using Aimy.Core.Application.DTOs;
using Aimy.Core.Application.Interfaces;
using Aimy.Core.Domain.Entities;
using Aimy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aimy.Infrastructure.Repositories;

public class UploadRepository(ApplicationDbContext context) : IUploadRepository
{
    public async Task<Upload> AddAsync(Upload upload, CancellationToken ct)
    {
        context.Uploads.Add(upload);
        await context.SaveChangesAsync(ct);
        return upload;
    }

    public async Task<Upload?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Uploads
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<IEnumerable<Upload>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await context.Uploads
            .Where(u => u.UserId == userId)
            .OrderByDescending(u => u.DateUploaded)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Upload>> GetByUserIdAndFileNameAsync(Guid userId, string fileName, CancellationToken ct)
    {
        return await context.Uploads
            .Where(u => u.UserId == userId && u.FileName == fileName)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Upload>> GetPagedAsync(Guid userId, int page, int pageSize, CancellationToken ct)
    {
        var query = context.Uploads.Where(u => u.UserId == userId);
        
        var totalCount = await query.CountAsync(ct);
        
        var items = await query
            .OrderByDescending(u => u.DateUploaded)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        
        return new PagedResult<Upload>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var upload = await context.Uploads.FindAsync([id], ct);
        if (upload != null)
        {
            context.Uploads.Remove(upload);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateAsync(Upload upload, CancellationToken ct)
    {
        context.Uploads.Update(upload);
        await context.SaveChangesAsync(ct);
    }
}
