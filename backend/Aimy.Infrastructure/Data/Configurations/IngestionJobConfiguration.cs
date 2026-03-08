using Aimy.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aimy.Infrastructure.Data.Configurations;

public class IngestionJobConfiguration : IEntityTypeConfiguration<IngestionJob>
{
    public void Configure(EntityTypeBuilder<IngestionJob> builder)
    {
        builder.ToTable("ingestion_jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.UploadId)
            .IsRequired();

        builder.Property(j => j.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(j => j.Attempts)
            .IsRequired();

        builder.Property(j => j.NextAttemptAt)
            .IsRequired();

        builder.Property(j => j.StartedAt)
            .HasColumnName("started_at");

        builder.Property(j => j.ClaimedAt);

        builder.Property(j => j.CompletedAt);

        builder.Property(j => j.LastError)
            .HasColumnType("text");

        builder.Property(j => j.CreatedAt)
            .IsRequired();

        builder.Property(j => j.UpdatedAt)
            .IsRequired();

        builder.HasOne(j => j.Upload)
            .WithMany()
            .HasForeignKey(j => j.UploadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(j => j.UploadId);
        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.NextAttemptAt);

        builder.HasIndex(j => j.UploadId)
            .HasFilter("\"Status\" IN (0, 1)")
            .IsUnique();
    }
}
