using Microsoft.Extensions.AI;
using Microsoft.Extensions.DataIngestion;

namespace Aimy.Infrastructure.Ingestion;

public interface IVectorStoreWriterFactory
{
    VectorStoreWriter<string> Create(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, int dimensions);
}
