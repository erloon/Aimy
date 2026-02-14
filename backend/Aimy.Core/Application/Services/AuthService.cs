namespace Aimy.Core.Application.Services;

using Aimy.Core.Application.Interfaces;
using Aimy.Core.Domain.Entities;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider)
    : IAuthService
{
    public async Task<string?> LoginAsync(string username, string password)
    {
        var user = await userRepository.GetByUsernameAsync(username);
        if (user is null)
            return null;

        if (!passwordHasher.Verify(password, user.PasswordHash))
            return null;

        return tokenProvider.GenerateToken(user);
    }
}
