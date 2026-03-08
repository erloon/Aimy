using System.Reflection;
using Aimy.API.Endpoints;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Aimy.Tests.Endpoints;

[TestFixture]
public class MetadataEndpointsContractTests
{
    [Test]
    public void MetadataEndpoints_Should_Not_Use_Object_In_Ok_Responses()
    {
        var endpointMethods = typeof(MetadataEndpoints)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(method => method.ReturnType.IsGenericType
                && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            .Where(method => method.ReturnType.GetGenericArguments()[0].IsGenericType
                && method.ReturnType.GetGenericArguments()[0].Name.StartsWith("Results`", StringComparison.Ordinal))
            .ToList();

        endpointMethods.Should().NotBeEmpty();

        foreach (var method in endpointMethods)
        {
            var resultsType = method.ReturnType.GetGenericArguments()[0];
            var okResultType = resultsType.GetGenericArguments()
                .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(Ok<>));

            okResultType.Should().NotBeNull($"{method.Name} should return Ok<T> as part of typed Results");

            var payloadType = okResultType!.GetGenericArguments()[0];
            payloadType.Should().NotBe(typeof(object), $"{method.Name} should use a dedicated API response model");
        }
    }
}
