namespace Aimy.Core.Application.DTOs;

public class UploadFileResponse
{
    public Guid Id { get; set; }
    public required string FileName { get; set; }
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public required string Link { get; set; }
    public string? Metadata { get; set; } // JSON string for key-value metadata
}
