using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Application.Interfaces.Upload;

namespace Aimy.Infrastructure.Repositories;

using Aimy.Core.Application.Interfaces;
using Aimy.Core.Domain.Entities;
using Aimy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class KnowledgeBaseRepository(ApplicationDbContext context) : IKnowledgeBaseRepository
{
    public async Task<KnowledgeBase> GetOrCreateForUserAsync(Guid userId, CancellationToken ct)
    {
        var kb = await context.KnowledgeBases
            .FirstOrDefaultAsync(kb => kb.UserId == userId, ct);
        
        if (kb != null)
            return kb;
        
        kb = new KnowledgeBase { UserId = userId };
        context.KnowledgeBases.Add(kb);
        await context.SaveChangesAsync(ct);
        return kb;
    }

    public async Task<KnowledgeBase?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await context.KnowledgeBases
            .FirstOrDefaultAsync(kb => kb.Id == id, ct);
    }

    public async Task<KnowledgeBase?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await context.KnowledgeBases
            .FirstOrDefaultAsync(kb => kb.UserId == userId, ct);
    }
}
