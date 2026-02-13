namespace Aimy.Core.Application.Interfaces;

using Aimy.Core.Domain.Entities;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task AddAsync(User user);
}
