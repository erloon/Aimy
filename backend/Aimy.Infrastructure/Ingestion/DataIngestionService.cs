using Aimy.Core.Application.Interfaces.Ingestion;
using Aimy.Core.Application.Interfaces.Upload;
using Aimy.Infrastructure.Data;
using Aimy.Core.Application.DTOs.Upload;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

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

        await writer.WriteAsync(chunks, cancellationToken);
    }

    public async Task DeleteByUploadIdAsync(Guid uploadId, CancellationToken cancellationToken)
    {
        await dbContext.IngestionEmbeddings
            .Where(chunk => chunk.SourceId == uploadId.ToString())
            .ExecuteDeleteAsync(cancellationToken);
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
}
