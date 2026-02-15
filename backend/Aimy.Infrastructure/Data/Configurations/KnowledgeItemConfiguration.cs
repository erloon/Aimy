using Aimy.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aimy.Infrastructure.Data.Configurations;

public class KnowledgeItemConfiguration : IEntityTypeConfiguration<KnowledgeItem>
{
    public void Configure(EntityTypeBuilder<KnowledgeItem> builder)
    {
        builder.ToTable("knowledge_items");
        
        builder.HasKey(ki => ki.Id);
        
        // Required FK - folder is source of truth
        builder.Property(ki => ki.FolderId)
            .IsRequired();
        
        builder.Property(ki => ki.Title)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(ki => ki.ItemType)
            .IsRequired();
        
        builder.Property(ki => ki.Content)
            .HasColumnType("text"); // Markdown content
        
        builder.Property(ki => ki.Tags)
            .HasColumnType("jsonb"); // JSON array of tags
        
        builder.Property(ki => ki.CreatedAt)
            .IsRequired();
        
        builder.Property(ki => ki.UpdatedAt)
            .IsRequired();
        
        // Relationship: Item belongs to Folder
        builder.HasOne(ki => ki.Folder)
            .WithMany(f => f.Items)
            .HasForeignKey(ki => ki.FolderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Relationship: Item may link to Upload
        builder.HasOne(ki => ki.SourceUpload)
            .WithMany()
            .HasForeignKey(ki => ki.SourceUploadId)
            .OnDelete(DeleteBehavior.SetNull); // Don't delete item if upload is deleted
        
        // Indexes
        builder.HasIndex(ki => ki.FolderId);
        builder.HasIndex(ki => ki.SourceUploadId);
        builder.HasIndex(ki => ki.ItemType);
    }
}
