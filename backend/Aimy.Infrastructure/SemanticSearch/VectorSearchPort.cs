using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Infrastructure.Configuration;
using Aimy.Infrastructure.Data.Entities;
using Aimy.Infrastructure.Ingestion;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.PgVector;
using OpenAI;
using IConfigurationProvider = Aimy.Core.Application.Interfaces.Integrations.IConfigurationProvider;

namespace Aimy.Infrastructure.SemanticSearch;

public class VectorSearchPort(
    IConfigurationProvider configurationProvider, 
    IConfiguration configuration,
    IOptions<IngestionOptions> options) : IVectorSearchPort
{
    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string query, int maxResults, double scoreThreshold, CancellationToken ct)
    {
        var connectionString = configuration.GetConnectionString("aimydb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'aimydb' is not configured.");
        }
        
        using PostgresVectorStore vectorStore = new(connectionString);
        var settings = options.Value;
        var collectionExists = await vectorStore.CollectionExistsAsync(settings.CollectionName, ct);

        if (!collectionExists)
        {
            throw new InvalidOperationException($"Collection '{options.Value.CollectionName}' does not exist.");
        }
        
        var collection =  vectorStore.GetCollection<string, IngestionEmbeddingRecord>(settings.CollectionName);
        
        var openAiOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(configurationProvider.GetOpenrouterEndpoint())
        };
        var openAiClient = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential(configurationProvider.GetOpenrouterApiKey()),
            openAiOptions);
        var embeddingGenerator = openAiClient.GetEmbeddingClient(settings.EmbeddingModel).AsIEmbeddingGenerator(VectorStoreSchema.EmbeddingDimensions);

        var queryVector = await embeddingGenerator.GenerateVectorAsync(query, cancellationToken: ct);
        
        var rawSearch = await collection.SearchAsync(queryVector, maxResults, cancellationToken: ct).ToListAsync(ct);
        
        var results = rawSearch.Where(r => r.Score >= scoreThreshold)
            .Select(r => new VectorSearchResult(r.Record.SourceId, r.Score))
            .ToList();
        
        return results;
    }
}