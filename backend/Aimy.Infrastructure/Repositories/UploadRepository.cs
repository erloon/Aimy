using Aimy.Core.Application.DTOs;
using Aimy.Core.Application.Interfaces;
using Aimy.Core.Domain.Entities;
using Aimy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aimy.Infrastructure.Repositories;

public class UploadRepository : IUploadRepository
{
    private readonly ApplicationDbContext _context;

    public UploadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Upload> AddAsync(Upload upload, CancellationToken ct)
    {
        _context.Uploads.Add(upload);
        await _context.SaveChangesAsync(ct);
        return upload;
    }

    public async Task<Upload?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Uploads
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<IEnumerable<Upload>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await _context.Uploads
            .Where(u => u.UserId == userId)
            .OrderByDescending(u => u.DateUploaded)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Upload>> GetByUserIdAndFileNameAsync(Guid userId, string fileName, CancellationToken ct)
    {
        return await _context.Uploads
            .Where(u => u.UserId == userId && u.FileName == fileName)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Upload>> GetPagedAsync(Guid userId, int page, int pageSize, CancellationToken ct)
    {
        var query = _context.Uploads.Where(u => u.UserId == userId);
        
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
        var upload = await _context.Uploads.FindAsync([id], ct);
        if (upload != null)
        {
            _context.Uploads.Remove(upload);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateAsync(Upload upload, CancellationToken ct)
    {
        _context.Uploads.Update(upload);
        await _context.SaveChangesAsync(ct);
    }
}
