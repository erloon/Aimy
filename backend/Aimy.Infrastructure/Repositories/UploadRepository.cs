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
}
