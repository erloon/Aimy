namespace Aimy.Core.Domain.Entities;

public enum UploadIngestionStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public enum IngestionJobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}
