# RAG Ingestion Storage

This document describes how ingestion embeddings are stored and mapped for the RAG pipeline.

## Storage Model

Embeddings are persisted in Postgres using the pgvector extension. The EF Core entity lives in **Infrastructure** and is mapped to the `ingestion_embeddings` table.

- Entity: `Aimy.Infrastructure.Data.Entities.IngestionEmbeddingRecord`
- Table: `ingestion_embeddings`
- Vector column: `Embedding` (type `vector(3072)`)
- Document id column: `DocumentId` (stored as `documentid` for vector store compatibility)
- Primary key column: `Id` (stored as `key` for vector store compatibility)
- Optional context column: `Context` (stored as `context`)
- Optional summary column: `Summary` (stored as `summary`, produced by `SummaryEnricher`)
- Optional metadata column: `Metadata` (stored as `metadata`, JSONB)

## Pipeline Configuration

Ingestion pipeline settings are now configurable via `Ingestion` options (see `Aimy.Infrastructure.Configuration.IngestionOptions`). This allows swapping models and enabling/disabling enrichers without code changes.

Example `appsettings.json`:

```json
{
  "Ingestion": {
    "ChatModel": "minimax/minimax-m2.5",
    "EmbeddingModel": "openai/text-embedding-3-large",
    "MaxTokensPerChunk": 2000,
    "OverlapTokens": 100,
    "CollectionName": "ingestion_embeddings",
    "DistanceFunction": "CosineSimilarity",
    "IndexKind": null,
    "IncrementalIngestion": true,
    "EnableSummary": true,
    "EnableImageAltText": true,
    "SummaryMaxWordCount": 100,
    "VectorStoreProvider": "pgvector"
  }
}
```

## Vector Store Factory

Vector store access is created through `IVectorStoreWriterFactory`. The current implementation is `PgVectorStoreWriterFactory`, which reads connection settings and ingestion options, then builds a `VectorStoreWriter` for the configured collection. To add a new store, implement a new factory and register it in DI.

## EF Core Mapping

EF Core maps the vector column using pgvector's `Vector` type with a value converter. The converter bridges `ReadOnlyMemory<float>?` (used by VectorData annotations) to `Pgvector.Vector` (the storage type). This keeps Core free of external dependencies and keeps vector persistence inside Infrastructure.

## Migrations

Use the .NET 10 SDK to keep the EF Core tooling version aligned with the runtime. With `global.json` at the repo root, run:

```bash
dotnet ef migrations add AddIngestionEmbeddings \
  --project backend/Aimy.Infrastructure \
  --startup-project backend/Aimy.API
```

## Notes

- The `vector` extension must be enabled in the database.
- The Aspire `AddPostgresVectorStore("aimydb")` registration already applies `UseVector()` when it builds the data source.
- `ImageAlternativeTextEnricher` does **not** require a dedicated column. It enriches document images with alternative text, and the chunker uses this semantic content when producing embeddings. That means image content is already searchable via vector similarity without extra schema changes.
- If you want to **display** or **filter** by image alt text later, add a custom document processor or chunk processor to copy alternative text into chunk metadata (e.g., `image_alt_text`) and map it to a column.

## Upload Integration

Upload responses now expose ingestion data in a developer-friendly shape:

- Top-level `ingestion.summary` as the main summary.
- `ingestion.chunks` with business-relevant chunk fields.
- Chunk payload excludes technical vector-store fields (`sourceid`, `documentid`, `embedding`), but includes chunk id.

### Metadata inheritance from upload

During ingestion, each chunk automatically receives system metadata and upload metadata:

- `sourceid`: upload id (`upload.Id.ToString()`).
- `createdat`: chunk creation timestamp (UTC).
- `upload_metadata`: parsed JSON object from `Upload.Metadata` when valid JSON.

`Upload.Metadata` is now canonicalized by the metadata governance pipeline before ingestion updates. This guarantees chunk-level `upload_metadata` receives normalized values for controlled keys (exact/alias/fuzzy resolution) while preserving permissive custom values when allowed.

> **Note for semantic search**: The `sourceid` field is the bridge between the ingestion pipeline and the semantic search pipeline. When semantic search retrieves vector results, it parses `sourceid` back to a `Guid` and queries `KnowledgeItem.SourceUploadId` to hydrate the full item response. See [Semantic Search](semantic-search.md) for details.

This keeps user-defined metadata consistent across upload and chunk records while allowing chunk-specific metadata enrichment.

When deleting an upload, the backend also removes ingestion chunks linked by `sourceid = uploadId.ToString()`.

Deletion is blocked when the upload is assigned to a knowledge base item.

## Durable Job Lifecycle

Ingestion orchestration now uses a DB-backed job queue (`ingestion_jobs`) instead of in-memory channels.

- Worker: `Aimy.Infrastructure.BackgroundJobs.UploadProcessingWorker`
- Claim path: atomic SQL claim in `Aimy.Infrastructure.Repositories.IngestionJobRepository`
- Source of truth: `ingestion_jobs` + upload lifecycle fields in `uploads`

### Upload lifecycle fields

Each upload now carries ingestion lifecycle visibility fields:

- `ingestionStatus`: `Pending | Processing | Completed | Failed`
- `ingestionError`: latest processing error (for failed states)
- `ingestionStartedAt`: UTC timestamp when processing started
- `ingestionCompletedAt`: UTC timestamp when processing finished

### Retry behavior

Retry is deterministic and bounded via `Ingestion` options:

- `MaxJobAttempts` (default `3`)
- `RetryDelaySeconds` (default `30`)

When a job fails:

1. Attempts are incremented.
2. If attempts are below max, job returns to `Pending` with `NextAttemptAt`.
3. If max attempts is reached, job becomes terminal `Failed`.

### Idempotency guarantees

`DataIngestionService.IngestDataAsync` starts from deterministic state by deleting prior chunks for the same upload (`sourceid = uploadId`) before writing fresh ingestion output. Reprocessing the same upload therefore does not duplicate chunk records.

### Compensation boundaries

Upload creation paths now perform explicit compensation to avoid terminal partial state:

- Storage write succeeds but upload DB insert fails -> storage object is deleted.
- Upload DB insert succeeds but ingestion job enqueue fails -> upload record and storage object are deleted.

The same compensation rules are applied for markdown note creation flows that create upload records.

## Related Documentation

- [Semantic Search](semantic-search.md) — Query the vectors produced by this pipeline using the `SimpleSemanticSearch` endpoint.
- [Semantic Search API](../api/semantic-search.md) — API contract for the semantic search endpoint.
