using Aimy.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aimy.Infrastructure.Data.Configurations;

public class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> builder)
    {
        builder.ToTable("folders");
        
        builder.HasKey(f => f.Id);
        
        builder.Property(f => f.KnowledgeBaseId)
            .IsRequired();
        
        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(f => f.CreatedAt)
            .IsRequired();
        
        builder.Property(f => f.UpdatedAt)
            .IsRequired();
        
        // Self-referencing relationship for hierarchy
        builder.HasOne(f => f.ParentFolder)
            .WithMany(f => f.SubFolders)
            .HasForeignKey(f => f.ParentFolderId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete cycles
        
        // Relationship: Folder belongs to KnowledgeBase
        builder.HasOne(f => f.KnowledgeBase)
            .WithMany(kb => kb.Folders)
            .HasForeignKey(f => f.KnowledgeBaseId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(f => f.KnowledgeBaseId);
        builder.HasIndex(f => f.ParentFolderId);
    }
}
