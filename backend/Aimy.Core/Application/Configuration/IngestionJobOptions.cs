namespace Aimy.Core.Application.Configuration;

public sealed class IngestionJobOptions
{
    public const string SectionName = "Ingestion";

    public int MaxJobAttempts { get; set; } = 3;

    public int RetryDelaySeconds { get; set; } = 30;
}
