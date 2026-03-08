namespace Aimy.Core.Domain.Entities;

public class Upload
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string FileName { get; set; }
    public required string StoragePath { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public string? Metadata { get; set; } // JSON string for key-value metadata
    public DateTime DateUploaded { get; set; }

    public Upload()
    {
        Id = Guid.NewGuid();
        DateUploaded = DateTime.UtcNow;
    }
}
