namespace Aimy.Core.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<string?> LoginAsync(string username, string password);
}
