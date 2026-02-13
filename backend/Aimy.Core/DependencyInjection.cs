using Aimy.Core.Application.Interfaces;
using Aimy.Core.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aimy.Core;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddCore(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IUploadService, UploadService>();
        
        return builder;
    }
}
