namespace Aimy.Core.Domain.Entities;

public class IngestionJob
{
    public Guid Id { get; set; }
    public Guid UploadId { get; set; }
    public IngestionJobStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Upload? Upload { get; set; }

    public IngestionJob()
    {
        Id = Guid.NewGuid();
        Status = IngestionJobStatus.Pending;
        Attempts = 0;
        NextAttemptAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
