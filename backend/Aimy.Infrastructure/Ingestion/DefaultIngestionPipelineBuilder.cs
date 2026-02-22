using Aimy.Core.Domain.Entities;
using Aimy.Core.Application.Interfaces.Integrations;
using Aimy.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using OpenAI;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.Extensions.Options;
using Microsoft.ML.Tokenizers;

namespace Aimy.Infrastructure.Ingestion;

public sealed class DefaultIngestionPipelineBuilder(
    IOptions<IngestionOptions> options,
    IConfigurationProvider configurationProvider)
    : IIngestionPipelineBuilder
{
    public Task<IngestionPipelineComponents> BuildAsync(Upload upload, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4");
        var reader = new MarkItDownMcpReader(new Uri(configurationProvider.GetMcpUrl()));

        var chunkerOptions = new IngestionChunkerOptions(tokenizer)
        {
            MaxTokensPerChunk = settings.MaxTokensPerChunk,
            OverlapTokens = settings.OverlapTokens,
        };
        var chunker = new DocumentTokenChunker(chunkerOptions);

        var openAiOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(configurationProvider.GetOpenrouterEndpoint())
        };

        var openAiClient = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential(configurationProvider.GetOpenrouterApiKey()),
            openAiOptions);
        var chatClient = openAiClient.GetChatClient(settings.ChatModel).AsIChatClient();
        var embeddingGenerator = openAiClient.GetEmbeddingClient(settings.EmbeddingModel).AsIEmbeddingGenerator();

        var enricherOptions = new EnricherOptions(chatClient);

        var documentProcessors = new List<IngestionDocumentProcessor>();
        if (settings.EnableImageAltText)
        {
            documentProcessors.Add(new ImageAlternativeTextEnricher(enricherOptions));
        }

        var chunkProcessors = new List<IngestionChunkProcessor<string>>();
        if (settings.EnableSummary)
        {
            chunkProcessors.Add(new SummaryEnricher(enricherOptions, settings.SummaryMaxWordCount));
        }

        return Task.FromResult(new IngestionPipelineComponents
        {
            Reader = reader,
            Chunker = chunker,
            DocumentProcessors = documentProcessors,
            ChunkProcessors = chunkProcessors,
            EmbeddingGenerator = embeddingGenerator
        });
    }
}
