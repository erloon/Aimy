using Aimy.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aimy.Infrastructure.Data.Configurations;

public class MetadataDefinitionConfiguration : IEntityTypeConfiguration<MetadataDefinition>
{
    public void Configure(EntityTypeBuilder<MetadataDefinition> builder)
    {
        builder.ToTable("metadata_definitions");

        builder.HasKey(definition => definition.Id);

        builder.Property(definition => definition.Key)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(definition => definition.Label)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(definition => definition.ValueType)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(definition => definition.Policy)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(definition => definition.Filterable)
            .IsRequired();

        builder.Property(definition => definition.AllowFreeText)
            .IsRequired();

        builder.Property(definition => definition.Required)
            .IsRequired();

        builder.Property(definition => definition.IsActive)
            .IsRequired();

        builder.Property(definition => definition.CreatedAt)
            .IsRequired();

        builder.Property(definition => definition.UpdatedAt)
            .IsRequired();

        builder.HasIndex(definition => definition.Key)
            .IsUnique();
    }
}
