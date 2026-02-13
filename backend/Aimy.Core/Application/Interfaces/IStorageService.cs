namespace Aimy.Core.Application.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(
        Guid userId,
        string fileName,
        Stream fileStream,
        string? contentType,
        CancellationToken ct);
}
