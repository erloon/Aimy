using Aimy.Core.Application.Interfaces;
using Aimy.Infrastructure.Data;
using Aimy.Infrastructure.Repositories;
using Aimy.Infrastructure.Security;
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
        
        // Security adapters
        builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        builder.Services.AddScoped<ITokenProvider, JwtTokenProvider>();
        
        return builder;
    }
}
