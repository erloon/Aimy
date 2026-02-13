namespace Aimy.Core.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? GetCurrentUserId();
}
