using System.Reflection;
using Aimy.Core.Domain.Entities;
using Aimy.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aimy.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Upload> Uploads => Set<Upload>();
    public DbSet<KnowledgeBase> KnowledgeBases => Set<KnowledgeBase>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<KnowledgeItem> KnowledgeItems => Set<KnowledgeItem>();
    public DbSet<IngestionEmbeddingRecord> IngestionEmbeddings => Set<IngestionEmbeddingRecord>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.HasPostgresExtension("vector");
        base.OnModelCreating(modelBuilder);
    }
}
