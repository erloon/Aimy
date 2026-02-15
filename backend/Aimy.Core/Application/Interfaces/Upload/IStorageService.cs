namespace Aimy.Core.Application.Interfaces.Upload;

public interface IStorageService
{
    Task<string> UploadAsync(
        Guid userId,
        string fileName,
        Stream fileStream,
        string? contentType,
        CancellationToken ct);

    Task<Stream> DownloadAsync(string storagePath, CancellationToken ct);

    Task DeleteAsync(string storagePath, CancellationToken ct);
}
