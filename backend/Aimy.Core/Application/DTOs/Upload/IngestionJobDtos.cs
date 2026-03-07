namespace Aimy.Core.Application.DTOs.Upload;

public sealed class ClaimedIngestionJobDto
{
    public Guid JobId { get; set; }
    public Guid UploadId { get; set; }
    public int Attempts { get; set; }
}

public sealed class IngestionJobStatusResponse
{
    public Guid JobId { get; set; }
    public Guid UploadId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
