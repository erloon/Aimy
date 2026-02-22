using Aimy.Core.Application.Interfaces.Integrations;
using Aimy.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Aimy.Infrastructure.Integrations;

public sealed class ConfigurationProvider(
    IOptions<MarkitdownOptions> markitdownOptions,
    IOptions<OpenrouterOptions> openrouterOptions)
    : IConfigurationProvider
{
    public string GetMcpUrl()
    {
        var url = markitdownOptions.Value.McpUrl.TrimEnd('/');
        return url.EndsWith("/mcp", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"{url}/mcp";
    }

    public string GetOpenrouterEndpoint()
    {
        var endpoint = openrouterOptions.Value.Endpoint.TrimEnd('/');
        return NormalizeOpenrouterEndpoint(endpoint);
    }

    public string GetOpenrouterApiKey() => openrouterOptions.Value.ApiKey;

    private static string NormalizeOpenrouterEndpoint(string endpoint)
    {
        if (endpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint[..^"/chat/completions".Length];
        }

        if (endpoint.EndsWith("/embeddings", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint[..^"/embeddings".Length];
        }

        return endpoint;
    }
}
