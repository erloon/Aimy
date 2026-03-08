using Aimy.Infrastructure.Data.Entities;
using Aimy.Infrastructure.Ingestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace Aimy.Infrastructure.Data.Configurations;

public class IngestionEmbeddingConfiguration : IEntityTypeConfiguration<IngestionEmbeddingRecord>
{
    public void Configure(EntityTypeBuilder<IngestionEmbeddingRecord> builder)
    {
        builder.ToTable(VectorStoreSchema.CollectionName);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("key");

        builder.Property(e => e.SourceId)
            .IsRequired()
            .HasMaxLength(128)
            .HasColumnName("sourceid");

        builder.Property(e => e.Content)
            .IsRequired()
            .HasColumnName("content");

        builder.Property(e => e.DocumentId)
            .IsRequired()
            .HasMaxLength(128)
            .HasColumnName("documentid");

        builder.Property(e => e.Embedding)
            .HasColumnType($"vector({VectorStoreSchema.EmbeddingDimensions})")
            .HasConversion(
                embedding => embedding.HasValue
                    ? new Vector(embedding.Value.ToArray())
                    : null,
                vector => vector == null
                    ? null
                    : new ReadOnlyMemory<float>(vector.ToArray()))
            .HasColumnName("embedding");

        builder.Property(e => e.Context)
            .HasColumnName("context");

        builder.Property(e => e.Summary)
            .HasColumnName("summary");

        builder.Property(e => e.Metadata)
            .HasColumnType("text")
            .HasColumnName("metadata");

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnName("createdat");

        builder.HasIndex(e => e.SourceId);
        builder.HasIndex(e => e.DocumentId);
    }
}
