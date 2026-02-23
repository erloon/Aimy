using Aimy.Core.Application.Interfaces.Ingestion;
using Aimy.Core.Application.Interfaces.Upload;
using Aimy.Infrastructure.Data;
using Aimy.Core.Application.DTOs.Upload;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;

namespace Aimy.Infrastructure.Ingestion;

public class DataIngestionService(
    ApplicationDbContext dbContext,
    ILoggerFactory loggerFactory,
    IUploadRepository uploadRepository,
    IStorageService storageService,
    IIngestionPipelineBuilder pipelineBuilder,
    IVectorStoreWriterFactory vectorStoreWriterFactory) : IDataIngestionService
{
    public async Task IngestDataAsync(Guid uploadId, CancellationToken cancellationToken)
    {
        var upload = await uploadRepository.GetByIdAsync(uploadId, cancellationToken);
        if (upload is null)
        {
            throw new InvalidOperationException($"Upload '{uploadId}' was not found.");
        }
        var components = await pipelineBuilder.BuildAsync(upload, cancellationToken);
        using var writer = vectorStoreWriterFactory.Create(
            components.EmbeddingGenerator,
            VectorStoreSchema.EmbeddingDimensions);
        var fileStream = await storageService.DownloadAsync(upload.StoragePath, cancellationToken);
        var document = await components.Reader.ReadAsync(
            fileStream,
            identifier: upload.Id.ToString(),
            mediaType: upload.ContentType ?? "application/octet-stream",
            cancellationToken: cancellationToken);


        using IngestionPipeline<string> pipeline = new(
            components.Reader,
            components.Chunker,
            writer,
            loggerFactory: loggerFactory);

        foreach (var processor in components.DocumentProcessors)
        {
            pipeline.DocumentProcessors.Add(processor);
        }

        foreach (var processor in components.ChunkProcessors)
        {
            pipeline.ChunkProcessors.Add(processor);
        }

        foreach (var processor in pipeline.DocumentProcessors)
        {
            document = await processor.ProcessAsync(document, cancellationToken);
        }

        var uploadMetadata = ParseUploadMetadata(upload.Metadata);

        var chunks = components.Chunker.ProcessAsync(document, cancellationToken)
            .Select(chunk =>
            {
                chunk.Metadata["sourceid"] = upload.Id.ToString();
                chunk.Metadata["createdat"] = DateTime.UtcNow;
                return chunk;
            });
        foreach (var processor in pipeline.ChunkProcessors)
        {
            chunks = processor.ProcessAsync(chunks, cancellationToken);
        }

        chunks = chunks.Select(chunk =>
        {
            var existingMetadata = TryGetMetadataPayload(chunk.Metadata);
            var mergedMetadata = MergeChunkMetadata(existingMetadata, uploadMetadata);
            if (mergedMetadata is null)
            {
                chunk.Metadata.Remove("metadata");
            }
            else
            {
                chunk.Metadata["metadata"] = mergedMetadata;
            }

            PromoteSummaryMetadata(chunk.Metadata);
            return chunk;
        });

        await writer.WriteAsync(chunks, cancellationToken);
    }

    public async Task DeleteByUploadIdAsync(Guid uploadId, CancellationToken cancellationToken)
    {
        await dbContext.IngestionEmbeddings
            .Where(chunk => chunk.SourceId == uploadId.ToString())
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task UpdateMetadataByUploadIdAsync(Guid uploadId, string? metadata, CancellationToken cancellationToken)
    {
        var sourceId = uploadId.ToString();
        var chunks = await dbContext.IngestionEmbeddings
            .Where(chunk => chunk.SourceId == sourceId)
            .ToListAsync(cancellationToken);

        if (chunks.Count == 0)
        {
            return;
        }

        var uploadMetadata = ParseUploadMetadata(metadata);

        foreach (var chunk in chunks)
        {
            var existingJson = chunk.Metadata;
            chunk.Metadata = MergeChunkMetadata(existingJson, uploadMetadata);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UploadIngestionResponse?> GetByUploadIdAsync(Guid uploadId, CancellationToken cancellationToken)
    {
        var chunks = await dbContext.IngestionEmbeddings
            .Where(chunk => chunk.SourceId == uploadId.ToString())
            .OrderBy(chunk => chunk.CreatedAt)
            .Select(chunk => new UploadChunkResponse
            {
                Id = chunk.Id,
                Content = chunk.Content,
                Context = chunk.Context,
                Summary = chunk.Summary,
                Metadata = chunk.Metadata,
                CreatedAt = chunk.CreatedAt
            })
            .ToListAsync(cancellationToken);

        if (chunks.Count == 0)
        {
            return null;
        }

        var summary = chunks
            .Select(chunk => chunk.Summary)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

        return new UploadIngestionResponse
        {
            Summary = summary,
            Chunks = chunks
        };
    }

    private static string? ParseUploadMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return null;
        }

        try
        {
            using var jsonDocument = JsonDocument.Parse(metadata);
            return jsonDocument.RootElement.GetRawText();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? MergeChunkMetadata(string? existingMetadata, string? uploadMetadata)
    {
        JsonObject metadataObject;

        if (string.IsNullOrWhiteSpace(existingMetadata))
        {
            metadataObject = new JsonObject();
        }
        else
        {
            try
            {
                metadataObject = JsonNode.Parse(existingMetadata) as JsonObject ?? new JsonObject();
            }
            catch (JsonException)
            {
                metadataObject = new JsonObject();
            }
        }

        if (uploadMetadata is null)
        {
            metadataObject.Remove("upload_metadata");
        }
        else
        {
            metadataObject["upload_metadata"] = JsonNode.Parse(uploadMetadata);
        }

        return metadataObject.ToJsonString();
    }


    private static string? TryGetMetadataPayload(IDictionary<string, object> metadata)
    {
        if (!metadata.TryGetValue("metadata", out var metadataValue) || metadataValue is null)
        {
            return null;
        }

        if (metadataValue is string metadataText)
        {
            return metadataText;
        }

        if (metadataValue is JsonElement jsonElement)
        {
            return jsonElement.GetRawText();
        }

        try
        {
            return JsonSerializer.Serialize(metadataValue);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool TryConvertToString(object? value, out string? result)
    {
        result = value switch
        {
            null => null,
            string text => text,
            JsonElement element => element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : element.GetRawText(),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };

        return !string.IsNullOrWhiteSpace(result);
    }

    private static void PromoteSummaryMetadata(IDictionary<string, object> metadata)
    {
        if (metadata.TryGetValue("summary", out var existingSummary)
            && TryConvertToString(existingSummary, out _))
        {
            return;
        }

        foreach (var entry in metadata)
        {
            if (!entry.Key.Contains("summary", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryConvertToString(entry.Value, out var normalizedSummary))
            {
                metadata["summary"] = normalizedSummary!;
                return;
            }
        }
    }
}
