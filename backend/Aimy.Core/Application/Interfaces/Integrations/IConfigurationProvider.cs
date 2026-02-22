namespace Aimy.Core.Application.Interfaces.Integrations;

public interface IConfigurationProvider
{
    string GetMcpUrl();
    string GetOpenrouterEndpoint();
    string GetOpenrouterApiKey();
}
