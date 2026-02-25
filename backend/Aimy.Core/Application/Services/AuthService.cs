using Aimy.Core.Application.Interfaces.Auth;

namespace Aimy.Core.Application.Services;

using Aimy.Core.Application.Interfaces;
using Aimy.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider,
    ILogger<AuthService> logger)
    : IAuthService
{
    public async Task<string?> LoginAsync(string username, string password)
    {
        logger.LogInformation("Login attempt for user {Username}", username);

        var user = await userRepository.GetByUsernameAsync(username);
        if (user is null)
        {
            logger.LogWarning("Login failed — user {Username} not found", username);
            return null;
        }

        if (!passwordHasher.Verify(password, user.PasswordHash))
        {
            logger.LogWarning("Login failed — invalid password for user {Username}", username);
            return null;
        }

        var token = tokenProvider.GenerateToken(user);
        logger.LogInformation("Login successful for user {Username} (UserId: {UserId})", username, user.Id);
        return token;
    }
}
