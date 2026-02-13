namespace Aimy.Core.Application.Interfaces;

using Aimy.Core.Domain.Entities;

public interface ITokenProvider
{
    string GenerateToken(User user);
}
