using Aimy.Core.Application.Interfaces.Auth;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Application.Interfaces.Upload;
using Aimy.Infrastructure.BackgroundJobs;
using Aimy.Infrastructure.Data;
using Aimy.Infrastructure.Messaging;
using Aimy.Infrastructure.Repositories;
using Aimy.Infrastructure.Security;
using Aimy.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aimy.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        // Database
        builder.AddNpgsqlDbContext<ApplicationDbContext>("aimydb");
        
        // Repositories
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IUploadRepository, UploadRepository>();
        
        // Security adapters
        builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        builder.Services.AddScoped<ITokenProvider, JwtTokenProvider>();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        // Storage
        builder.Services.AddScoped<IStorageService, MinioStorageService>();
        
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
