using Aimy.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aimy.Infrastructure.Data.Configurations;

public class KnowledgeBaseConfiguration : IEntityTypeConfiguration<KnowledgeBase>
{
    public void Configure(EntityTypeBuilder<KnowledgeBase> builder)
    {
        builder.ToTable("knowledge_bases");
        
        builder.HasKey(kb => kb.Id);
        
        builder.Property(kb => kb.UserId)
            .IsRequired();
        
        // UNIQUE constraint: one KB per user
        builder.HasIndex(kb => kb.UserId)
            .IsUnique();
        
        builder.Property(kb => kb.CreatedAt)
            .IsRequired();
        
        builder.Property(kb => kb.UpdatedAt)
            .IsRequired();
        
        // Relationship: User has one KnowledgeBase
        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<KnowledgeBase>(kb => kb.UserId);
    }
}
