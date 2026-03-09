using Aimy.API.Models;
using Aimy.Core.Application.DTOs.Upload;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Aimy.API.Endpoints;

public static class IngestionEndpoints
{
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/kb/ingestion")
            .WithTags("Knowledge Base - Ingestion")
            .RequireAuthorization();

        group.MapGet("/jobs", ListIngestionJobs)
            .WithName("ListIngestionJobs")
            .WithSummary("List ingestion jobs for operational visibility")
            .Produces<IReadOnlyList<IngestionJobStatusResponse>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/jobs/{jobId}/retry", RetryIngestionJob)
            .WithName("RetryIngestionJob")
            .WithSummary("Retry a failed ingestion job")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<Results<Ok<IReadOnlyList<IngestionJobStatusResponse>>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, ProblemHttpResult>> ListIngestionJobs(
        IIngestionJobService ingestionJobService,
        string? status,
        int limit = 50,
        CancellationToken ct = default)
    {
        if (limit < 1 || limit > 200)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Limit must be between 1 and 200" });
        }

        try
        {
            var result = await ingestionJobService.ListAsync(status, limit, ct);
            return TypedResults.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to list ingestion jobs");
        }
    }

    private static async Task<Results<NoContent, BadRequest<ErrorResponse>, UnauthorizedHttpResult, NotFound, ProblemHttpResult>> RetryIngestionJob(
        Guid jobId,
        IIngestionJobService ingestionJobService,
        CancellationToken ct)
    {
        try
        {
            var retried = await ingestionJobService.RetryAsync(jobId, ct);
            if (!retried)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to retry ingestion job");
        }
    }
}
