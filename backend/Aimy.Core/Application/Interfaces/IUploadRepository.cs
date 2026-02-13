namespace Aimy.Core.Application.Interfaces;

using Aimy.Core.Application.DTOs;
using Aimy.Core.Domain.Entities;

public interface IUploadRepository
{
    Task<Upload> AddAsync(Upload upload, CancellationToken ct);
    Task<Upload?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<Upload>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<IEnumerable<Upload>> GetByUserIdAndFileNameAsync(Guid userId, string fileName, CancellationToken ct);
    Task<PagedResult<Upload>> GetPagedAsync(Guid userId, int page, int pageSize, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task UpdateAsync(Upload upload, CancellationToken ct);
}
