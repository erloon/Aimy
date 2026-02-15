using Aimy.Core.Application.DTOs;
using Aimy.Core.Application.DTOs.KnowledgeBase;

namespace Aimy.Core.Application.Interfaces.KnowledgeBase;

public interface IKnowledgeItemService
{
    Task<ItemResponse> CreateNoteAsync(CreateNoteRequest request, CancellationToken ct);
    Task<ItemResponse> CreateFromUploadAsync(CreateItemFromUploadRequest request, CancellationToken ct);
    Task<ItemResponse> UpdateAsync(Guid id, UpdateItemRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<ItemResponse?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PagedResult<ItemResponse>> SearchAsync(ItemSearchRequest request, CancellationToken ct);
}
