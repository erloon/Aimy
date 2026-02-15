using Aimy.Core.Domain.Entities;

namespace Aimy.Core.Application.Interfaces.Auth;

public interface ITokenProvider
{
    string GenerateToken(User user);
}
