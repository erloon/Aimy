using Aimy.API.Models;
using Aimy.Core.Application.Interfaces;

namespace Aimy.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Authentication");
        
        group.MapPost("/login", async (
            LoginRequest request,
            IAuthService authService) =>
        {
            var token = await authService.LoginAsync(request.Username, request.Password);
            return token is not null
                ? Results.Ok(new { token })
                : Results.Unauthorized();
        })
        .WithName("Login");
        
        return app;
    }   
}