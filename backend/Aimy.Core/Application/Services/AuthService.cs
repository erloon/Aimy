namespace Aimy.Core.Application.Services;

using Aimy.Core.Application.Interfaces;
using Aimy.Core.Domain.Entities;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenProvider tokenProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenProvider = tokenProvider;
    }

    public async Task<string?> LoginAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user is null)
            return null;

        if (!_passwordHasher.Verify(password, user.PasswordHash))
            return null;

        return _tokenProvider.GenerateToken(user);
    }
}
