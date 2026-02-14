using Aimy.API.Models;
using Aimy.Core.Application.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Aimy.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Authentication");

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Authenticate user and obtain JWT token")
            .WithDescription("Authenticates a user with username and password credentials and returns a JWT token for subsequent API requests.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="authService">Authentication service</param>
    /// <returns>JWT token on success, Unauthorized on failure</returns>
    /// <response code="200">Returns the JWT authentication token</response>
    /// <response code="400">Invalid request format</response>
    /// <response code="401">Invalid credentials</response>
    private static async Task<Results<Ok<LoginResponse>, UnauthorizedHttpResult>> Login(
        LoginRequest request,
        IAuthService authService)
    {
        var token = await authService.LoginAsync(request.Username, request.Password);
        return token is not null
            ? TypedResults.Ok(new LoginResponse { Token = token })
            : TypedResults.Unauthorized();
    }
}