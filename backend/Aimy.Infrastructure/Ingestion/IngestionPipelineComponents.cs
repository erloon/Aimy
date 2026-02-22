using Microsoft.Extensions.AI;
using Microsoft.Extensions.DataIngestion;

namespace Aimy.Infrastructure.Ingestion;

public sealed class IngestionPipelineComponents
{
    public required IngestionDocumentReader Reader { get; init; }

    public required IngestionChunker<string> Chunker { get; init; }

    public required IReadOnlyList<IngestionDocumentProcessor> DocumentProcessors { get; init; }

    public required IReadOnlyList<IngestionChunkProcessor<string>> ChunkProcessors { get; init; }

    public required IEmbeddingGenerator<string, Embedding<float>> EmbeddingGenerator { get; init; }
}
