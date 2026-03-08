using Aimy.Core.Application.Interfaces.Auth;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Application.Interfaces.Upload;

namespace Aimy.Core.Application.Services;

using Aimy.Core.Application.DTOs.KnowledgeBase;
using Aimy.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;

public class KnowledgeBaseService(
    IKnowledgeBaseRepository kbRepository,
    ICurrentUserService currentUserService,
    ILogger<KnowledgeBaseService> logger) : IKnowledgeBaseService
{
    public async Task<KnowledgeBaseResponse> GetOrCreateAsync(CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated");

        logger.LogInformation("GetOrCreate knowledge base for user {UserId}", userId);

        var kb = await kbRepository.GetOrCreateForUserAsync(userId, ct);

        logger.LogInformation("Knowledge base {KnowledgeBaseId} resolved for user {UserId}", kb.Id, userId);

        return new KnowledgeBaseResponse
        {
            Id = kb.Id,
            UserId = kb.UserId,
            CreatedAt = kb.CreatedAt,
            UpdatedAt = kb.UpdatedAt
        };
    }
}
