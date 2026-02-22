using Aimy.Infrastructure.Ingestion;
using Microsoft.Extensions.VectorData;

namespace Aimy.Infrastructure.Data.Entities;

public class IngestionEmbeddingRecord
{
    [VectorStoreKey(StorageName = "key")] public Guid Id { get; set; }

    [VectorStoreData(IsIndexed = true, StorageName = "documentid")]
    public required string DocumentId { get; set; }

    [VectorStoreData(IsIndexed = true, StorageName = "sourceid")]
    public required string SourceId { get; set; }

    [VectorStoreData(StorageName = "content")] public required string Content { get; set; }

    [VectorStoreVector(Dimensions: VectorStoreSchema.EmbeddingDimensions, StorageName = "embedding")]
    public ReadOnlyMemory<float>? Embedding { get; set; }

    [VectorStoreData(StorageName = "context")] public string? Context { get; set; }

    [VectorStoreData(StorageName = "summary")] public string? Summary { get; set; }

    [VectorStoreData(StorageName = "metadata")] public string? Metadata { get; set; }

    [VectorStoreData(StorageName = "createdat")] public DateTime CreatedAt { get; set; }
}
