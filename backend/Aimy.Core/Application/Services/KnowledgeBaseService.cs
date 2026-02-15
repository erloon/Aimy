using Aimy.Core.Application.Interfaces.Auth;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Application.Interfaces.Upload;

namespace Aimy.Core.Application.Services;

using Aimy.Core.Application.DTOs.KnowledgeBase;
using Aimy.Core.Application.Interfaces;

public class KnowledgeBaseService(
    IKnowledgeBaseRepository kbRepository,
    ICurrentUserService currentUserService) : IKnowledgeBaseService
{
    public async Task<KnowledgeBaseResponse> GetOrCreateAsync(CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated");

        var kb = await kbRepository.GetOrCreateForUserAsync(userId, ct);

        return new KnowledgeBaseResponse
        {
            Id = kb.Id,
            UserId = kb.UserId,
            CreatedAt = kb.CreatedAt,
            UpdatedAt = kb.UpdatedAt
        };
    }
}
