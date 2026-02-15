using Aimy.Core.Application.DTOs;

namespace Aimy.Core.Application.Interfaces.Upload;

public interface IUploadRepository
{
    Task<Domain.Entities.Upload> AddAsync(Domain.Entities.Upload upload, CancellationToken ct);
    Task<Domain.Entities.Upload?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<Domain.Entities.Upload>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<IEnumerable<Domain.Entities.Upload>> GetByUserIdAndFileNameAsync(Guid userId, string fileName, CancellationToken ct);
    Task<PagedResult<Domain.Entities.Upload>> GetPagedAsync(Guid userId, int page, int pageSize, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task UpdateAsync(Domain.Entities.Upload upload, CancellationToken ct);
}
