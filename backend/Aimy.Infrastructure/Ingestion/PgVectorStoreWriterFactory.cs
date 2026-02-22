using Aimy.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.PgVector;

namespace Aimy.Infrastructure.Ingestion;

public sealed class PgVectorStoreWriterFactory(
    IConfiguration configuration,
    IOptions<IngestionOptions> options) : IVectorStoreWriterFactory
{
    public VectorStoreWriter<string> Create(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, int dimensions)
    {
        var connectionString = configuration.GetConnectionString("aimydb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'aimydb' is not configured.");
        }

        var settings = options.Value;
        if (!string.Equals(settings.VectorStoreProvider, "pgvector", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported vector store provider '{settings.VectorStoreProvider}'.");
        }

        var pgVectorStore = new PostgresVectorStore(connectionString, new PostgresVectorStoreOptions
        {
            EmbeddingGenerator = embeddingGenerator
        });

        return new VectorStoreWriter<string>(
            pgVectorStore,
            dimensions,
            new VectorStoreWriterOptions
            {
                CollectionName = settings.CollectionName,
                DistanceFunction = settings.DistanceFunction,
                IndexKind = settings.IndexKind,
                IncrementalIngestion = settings.IncrementalIngestion
            });
    }
}
