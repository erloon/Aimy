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
    "DistanceFunction": null,
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

This keeps user-defined metadata consistent across upload and chunk records while allowing chunk-specific metadata enrichment.

When deleting an upload, the backend also removes ingestion chunks linked by `sourceid = uploadId.ToString()`.

Deletion is blocked when the upload is assigned to a knowledge base item.
