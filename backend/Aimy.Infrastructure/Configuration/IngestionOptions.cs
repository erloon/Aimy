namespace Aimy.Infrastructure.Configuration;

public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    public string ChatModel { get; set; } = "minimax/minimax-m2.5";

    public string EmbeddingModel { get; set; } = "openai/text-embedding-3-large";

    public int MaxTokensPerChunk { get; set; } = 2000;

    public int OverlapTokens { get; set; } = 100;

    public string CollectionName { get; set; } = "ingestion_embeddings";

    public string? DistanceFunction { get; set; }

    public string? IndexKind { get; set; }

    public bool IncrementalIngestion { get; set; } = true;

    public bool EnableSummary { get; set; } = true;

    public bool EnableImageAltText { get; set; } = true;

    public int SummaryMaxWordCount { get; set; } = 100;

    public string VectorStoreProvider { get; set; } = "pgvector";
}
