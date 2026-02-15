using Aimy.Core.Domain.Entities;

namespace Aimy.Core.Application.Interfaces.Auth;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task AddAsync(User user);
}
