using System.Reflection;
using Aimy.API.Endpoints;
using Aimy.Core.Application.DTOs.Upload;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Aimy.Tests.Endpoints;

/// <summary>
/// Contract tests for IngestionEndpoints.
///
/// These tests verify:
///   - GET /kb/ingestion/jobs  →  Ok&lt;IReadOnlyList&lt;IngestionJobStatusResponse&gt;&gt; (ListIngestionJobs handler)
///   - POST /kb/ingestion/jobs/{jobId}/retry  →  NoContent or NotFound or BadRequest or Problem (RetryIngestionJob handler)
///
/// NOTE: The old routes /uploads/ingestion-jobs (GET) and /uploads/ingestion-jobs/{jobId}/retry (POST)
/// were removed from UploadEndpoints.cs in Task 9 as part of the storage-decoupling refactor.
/// UploadEndpoints.cs must NOT contain the string "ingestion-jobs" — verified by
/// OldIngestionRoutesRemovedFromUploadEndpoints_UploadEndpointsClass_ShouldNotDefineIngestionJobRoutes below.
/// </summary>
[TestFixture]
public class IngestionEndpointsContractTests
{
    // ─── ListIngestionJobs ────────────────────────────────────────────────────

    [Test]
    public void ListIngestionJobs_Should_Return_Ok_With_TypedIngestionJobStatusResponseList()
    {
        var method = GetPrivateStaticMethod("ListIngestionJobs");
        method.Should().NotBeNull("ListIngestionJobs handler must exist on IngestionEndpoints");

        var returnType = method!.ReturnType;
        returnType.IsGenericType.Should().BeTrue("return type should be Task<Results<...>>");
        returnType.GetGenericTypeDefinition().Should().Be(typeof(Task<>));

        var resultsType = returnType.GetGenericArguments()[0];
        resultsType.Name.Should().StartWith("Results`", "return type inner should be typed Results union");

        var okResultType = resultsType.GetGenericArguments()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Ok<>));

        okResultType.Should().NotBeNull("ListIngestionJobs must return Ok<T> as one of its typed results");

        var payloadType = okResultType!.GetGenericArguments()[0];
        payloadType.Should().NotBe(typeof(object), "ListIngestionJobs must use a dedicated response model, not object");

        // Verify the payload is IReadOnlyList<IngestionJobStatusResponse>
        payloadType.IsGenericType.Should().BeTrue("Ok<T> payload should be a generic collection");
        payloadType.GetGenericTypeDefinition().Should().Be(typeof(IReadOnlyList<>),
            "ListIngestionJobs should return Ok<IReadOnlyList<IngestionJobStatusResponse>>");
        payloadType.GetGenericArguments()[0].Should().Be(typeof(IngestionJobStatusResponse),
            "the collection element type must be IngestionJobStatusResponse");
    }

    [Test]
    public void ListIngestionJobs_Should_Include_BadRequest_TypedResult()
    {
        var method = GetPrivateStaticMethod("ListIngestionJobs");
        method.Should().NotBeNull();

        var resultsType = method!.ReturnType.GetGenericArguments()[0];
        var badRequestType = resultsType.GetGenericArguments()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(BadRequest<>));

        badRequestType.Should().NotBeNull("ListIngestionJobs must declare BadRequest<T> for invalid input");
    }

    [Test]
    public void ListIngestionJobs_Should_Include_ProblemHttpResult()
    {
        var method = GetPrivateStaticMethod("ListIngestionJobs");
        method.Should().NotBeNull();

        var resultsType = method!.ReturnType.GetGenericArguments()[0];
        var problemType = resultsType.GetGenericArguments()
            .FirstOrDefault(t => t == typeof(ProblemHttpResult));

        problemType.Should().NotBeNull("ListIngestionJobs must declare ProblemHttpResult for server errors");
    }

    // ─── RetryIngestionJob ────────────────────────────────────────────────────

    [Test]
    public void RetryIngestionJob_Should_Return_NoContent_TypedResult()
    {
        var method = GetPrivateStaticMethod("RetryIngestionJob");
        method.Should().NotBeNull("RetryIngestionJob handler must exist on IngestionEndpoints");

        var returnType = method!.ReturnType;
        returnType.IsGenericType.Should().BeTrue("return type should be Task<Results<...>>");
        returnType.GetGenericTypeDefinition().Should().Be(typeof(Task<>));

        var resultsType = returnType.GetGenericArguments()[0];
        resultsType.Name.Should().StartWith("Results`", "return type inner should be typed Results union");

        var noContentType = resultsType.GetGenericArguments()
            .FirstOrDefault(t => t == typeof(NoContent));

        noContentType.Should().NotBeNull("RetryIngestionJob must return NoContent on success");
    }

    [Test]
    public void RetryIngestionJob_Should_Include_NotFound_TypedResult()
    {
        var method = GetPrivateStaticMethod("RetryIngestionJob");
        method.Should().NotBeNull();

        var resultsType = method!.ReturnType.GetGenericArguments()[0];
        var notFoundType = resultsType.GetGenericArguments()
            .FirstOrDefault(t => t == typeof(NotFound));

        notFoundType.Should().NotBeNull("RetryIngestionJob must return NotFound when job does not exist");
    }

    [Test]
    public void RetryIngestionJob_Should_Include_BadRequest_TypedResult()
    {
        var method = GetPrivateStaticMethod("RetryIngestionJob");
        method.Should().NotBeNull();

        var resultsType = method!.ReturnType.GetGenericArguments()[0];
        var badRequestType = resultsType.GetGenericArguments()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(BadRequest<>));

        badRequestType.Should().NotBeNull("RetryIngestionJob must declare BadRequest<T> for invalid input");
    }

    [Test]
    public void RetryIngestionJob_Should_Include_ProblemHttpResult()
    {
        var method = GetPrivateStaticMethod("RetryIngestionJob");
        method.Should().NotBeNull();

        var resultsType = method!.ReturnType.GetGenericArguments()[0];
        var problemType = resultsType.GetGenericArguments()
            .FirstOrDefault(t => t == typeof(ProblemHttpResult));

        problemType.Should().NotBeNull("RetryIngestionJob must declare ProblemHttpResult for server errors");
    }

    // ─── Route verification ───────────────────────────────────────────────────

    [Test]
    public void IngestionEndpoints_Should_Have_Both_Handler_Methods()
    {
        var listMethod = GetPrivateStaticMethod("ListIngestionJobs");
        var retryMethod = GetPrivateStaticMethod("RetryIngestionJob");

        listMethod.Should().NotBeNull("ListIngestionJobs handler must be defined in IngestionEndpoints");
        retryMethod.Should().NotBeNull("RetryIngestionJob handler must be defined in IngestionEndpoints");
    }

    /// <summary>
    /// Verifies that the old /uploads/ingestion-jobs routes have been removed.
    /// The UploadEndpoints class must not contain any method whose name includes
    /// "Ingestion" (case-insensitive) — those handlers moved to IngestionEndpoints.
    /// </summary>
    [Test]
    public void OldIngestionRoutesRemovedFromUploadEndpoints_UploadEndpointsClass_ShouldNotDefineIngestionJobRoutes()
    {
        var uploadEndpointMethods = typeof(UploadEndpoints)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.Name.Contains("Ingestion", StringComparison.OrdinalIgnoreCase))
            .ToList();

        uploadEndpointMethods.Should().BeEmpty(
            "the old /uploads/ingestion-jobs routes were removed from UploadEndpoints in Task 9; " +
            "ingestion job management now lives exclusively under /kb/ingestion in IngestionEndpoints");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static MethodInfo? GetPrivateStaticMethod(string methodName) =>
        typeof(IngestionEndpoints)
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
}
