namespace Aimy.Core.Application.Interfaces.Auth;

public interface ICurrentUserService
{
    Guid? GetCurrentUserId();
}
