using Aimy.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aimy.Infrastructure.Data.Configurations;

public class MetadataValueOptionConfiguration : IEntityTypeConfiguration<MetadataValueOption>
{
    public void Configure(EntityTypeBuilder<MetadataValueOption> builder)
    {
        builder.ToTable("metadata_value_options");

        builder.HasKey(option => option.Id);

        builder.Property(option => option.MetadataDefinitionId)
            .IsRequired();

        builder.Property(option => option.CanonicalValue)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(option => option.DisplayLabel)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(option => option.Aliases)
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(option => option.IsActive)
            .IsRequired();

        builder.Property(option => option.SortOrder)
            .IsRequired();

        builder.Property(option => option.CreatedAt)
            .IsRequired();

        builder.Property(option => option.UpdatedAt)
            .IsRequired();

        builder.HasOne(option => option.Definition)
            .WithMany(definition => definition.ValueOptions)
            .HasForeignKey(option => option.MetadataDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(option => option.MetadataDefinitionId);
        builder.HasIndex(option => new { option.MetadataDefinitionId, option.CanonicalValue })
            .IsUnique();
        builder.HasIndex(option => option.CanonicalValue);
    }
}
