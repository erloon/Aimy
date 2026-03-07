using Aimy.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aimy.Infrastructure.Data.Configurations;

public class UploadConfiguration : IEntityTypeConfiguration<Upload>
{
    public void Configure(EntityTypeBuilder<Upload> builder)
    {
        builder.ToTable("uploads");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.UserId)
            .IsRequired();
        
        builder.Property(u => u.FileName)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(u => u.StoragePath)
            .IsRequired()
            .HasMaxLength(1000);
        
        builder.Property(u => u.FileSizeBytes)
            .IsRequired();
        
        builder.Property(u => u.ContentType)
            .HasMaxLength(256);
        
        builder.Property(u => u.Metadata)
            .HasColumnType("jsonb");

        builder.Property(u => u.SourceMarkdown)
            .HasColumnType("text");

        builder.Property(u => u.IngestionStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(u => u.IngestionError)
            .HasColumnType("text");

        builder.Property(u => u.IngestionStartedAt);

        builder.Property(u => u.IngestionCompletedAt);
        
        builder.Property(u => u.DateUploaded)
            .IsRequired();
        
        // Index for querying uploads by user
        builder.HasIndex(u => u.UserId);
        builder.HasIndex(u => u.IngestionStatus);
        
        // Composite index for duplicate filename checking
        builder.HasIndex(u => new { u.UserId, u.FileName });
    }
}
