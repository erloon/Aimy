using Aimy.Core.Application.Interfaces.Auth;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Application.Interfaces.Upload;
using Aimy.Core.Application.Interfaces.Integrations;
using Aimy.Infrastructure.BackgroundJobs;
using Aimy.Infrastructure.Integrations;
using Aimy.Infrastructure.Data;
using Aimy.Infrastructure.Ingestion;
using Aimy.Infrastructure.Messaging;
using Aimy.Infrastructure.Repositories;
using Aimy.Infrastructure.Security;
using Aimy.Infrastructure.Storage;
using Aimy.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Aimy.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<MarkitdownOptions>()
            .Bind(builder.Configuration.GetSection(MarkitdownOptions.SectionName))
            .PostConfigure(options =>
            {
                var mcpUrl = builder.Configuration["MARKITDOWN_MCP_URL"];
                if (!string.IsNullOrWhiteSpace(mcpUrl))
                {
                    options.McpUrl = mcpUrl;
                }
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.McpUrl), "MARKITDOWN_MCP_URL (or Markitdown:McpUrl) is required.");

        builder.Services.AddOptions<OpenrouterOptions>()
            .Bind(builder.Configuration.GetSection(OpenrouterOptions.SectionName))
            .PostConfigure(options =>
            {
                var apiKey = builder.Configuration["OPENROUTER_API_KEY"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    options.ApiKey = apiKey;
                }
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.Endpoint), "Openrouter:Endpoint is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "OPENROUTER_API_KEY (or Openrouter:ApiKey) is required.");

        builder.Services.AddOptions<IngestionOptions>()
            .Bind(builder.Configuration.GetSection(IngestionOptions.SectionName));

        // Database
        builder.AddNpgsqlDbContext<ApplicationDbContext>(
            "aimydb",
            configureDbContextOptions: options => options.UseNpgsql(o =>
            {
                o.UseVector();
                o.ConfigureDataSource(ds => ds.EnableDynamicJson());
            }));
        builder.Services.AddPostgresVectorStore("aimydb");
        // Repositorie
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IUploadRepository, UploadRepository>();
        
        // Security adapters
        builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        builder.Services.AddScoped<ITokenProvider, JwtTokenProvider>();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<
            Aimy.Core.Application.Interfaces.Ingestion.IDataIngestionService,
            DataIngestionService>();
        builder.Services.AddScoped<IIngestionPipelineBuilder, DefaultIngestionPipelineBuilder>();
        builder.Services.AddScoped<IVectorStoreWriterFactory, PgVectorStoreWriterFactory>();
        // Storage
        builder.Services.AddScoped<IStorageService, MinioStorageService>();

        // Integrations
        builder.Services.AddScoped<IConfigurationProvider, ConfigurationProvider>();
        
        // Knowledge Base repositories
        builder.Services.AddScoped<IKnowledgeBaseRepository, KnowledgeBaseRepository>();
        builder.Services.AddScoped<IFolderRepository, FolderRepository>();
        builder.Services.AddScoped<IKnowledgeItemRepository, KnowledgeItemRepository>();
        
        // Messaging
        var channel = new InMemoryUploadChannel();
        builder.Services.AddSingleton<IUploadQueueWriter>(channel);
        builder.Services.AddSingleton<IUploadQueueReader>(channel);
        builder.Services.AddHostedService<UploadProcessingWorker>();
        return builder;
    }
}
