using Aimy.API.Models;
using Aimy.Core.Application.DTOs.KnowledgeBase;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Aimy.API.Endpoints;

public static class FolderEndpoints
{
    public static IEndpointRouteBuilder MapFolderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/kb/folders")
            .WithTags("Knowledge Base - Folders")
            .RequireAuthorization();

        group.MapGet("/tree", GetTree)
            .WithName("GetFolderTree")
            .WithSummary("Get the folder tree for the current user")
            .Produces<FolderTreeResponse>()
            .Produces<UnauthorizedHttpResult>()
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/", CreateFolder)
            .WithName("CreateFolder")
            .WithSummary("Create a new folder")
            .Accepts<CreateFolderRequest>("application/json")
            .Produces<FolderResponse>(StatusCodes.Status201Created)
            .Produces<BadRequest<ErrorResponse>>()
            .Produces<UnauthorizedHttpResult>()
            .Produces<NotFound>()
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPut("/{id}", UpdateFolder)
            .WithName("UpdateFolder")
            .WithSummary("Update a folder's name")
            .Accepts<UpdateFolderRequest>("application/json")
            .Produces<FolderResponse>()
            .Produces<BadRequest<ErrorResponse>>()
            .Produces<UnauthorizedHttpResult>()
            .Produces<NotFound>()
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id}/content-summary", GetContentSummary)
            .WithName("GetFolderContentSummary")
            .WithSummary("Get recursive content summary for a folder")
            .Produces<FolderContentSummary>()
            .Produces<UnauthorizedHttpResult>()
            .Produces<NotFound>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{id}", DeleteFolder)
            .WithName("DeleteFolder")
            .WithSummary("Delete a folder (must be empty unless force=true)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<BadRequest<ErrorResponse>>()
            .Produces<UnauthorizedHttpResult>()
            .Produces<NotFound>()
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/{id}/move", MoveFolder)
            .WithName("MoveFolder")
            .WithSummary("Move a folder to a new parent")
            .Accepts<MoveFolderRequest>("application/json")
            .Produces<FolderResponse>()
            .Produces<BadRequest<ErrorResponse>>()
            .Produces<UnauthorizedHttpResult>()
            .Produces<NotFound>()
            .Produces(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<Results<Ok<FolderTreeResponse>, UnauthorizedHttpResult, ProblemHttpResult>> GetTree(
        IFolderService folderService,
        CancellationToken ct)
    {
        try
        {
            var result = await folderService.GetTreeAsync(ct);
            return TypedResults.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<Results<Created<FolderResponse>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, NotFound, ProblemHttpResult>> CreateFolder(
        CreateFolderRequest request,
        IFolderService folderService,
        CancellationToken ct)
    {
        try
        {
            var result = await folderService.CreateAsync(request, ct);
            return TypedResults.Created($"/kb/folders/{result.Id}", result);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<Results<Ok<FolderResponse>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, NotFound, ProblemHttpResult>> UpdateFolder(
        Guid id,
        UpdateFolderRequest request,
        IFolderService folderService,
        CancellationToken ct)
    {
        try
        {
            var result = await folderService.UpdateAsync(id, request, ct);
            return TypedResults.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<Results<NoContent, BadRequest<ErrorResponse>, UnauthorizedHttpResult, NotFound, ProblemHttpResult>> DeleteFolder(
        Guid id,
        IFolderService folderService,
        CancellationToken ct,
        bool force = false)
    {
        try
        {
            await folderService.DeleteAsync(id, force, ct);
            return TypedResults.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<Results<Ok<FolderContentSummary>, UnauthorizedHttpResult, NotFound, ProblemHttpResult>> GetContentSummary(
        Guid id,
        IFolderService folderService,
        CancellationToken ct)
    {
        try
        {
            var result = await folderService.GetContentSummaryAsync(id, ct);
            return TypedResults.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<Results<Ok<FolderResponse>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, NotFound, ProblemHttpResult>> MoveFolder(
        Guid id,
        MoveFolderRequest request,
        IFolderService folderService,
        CancellationToken ct)
    {
        try
        {
            var result = await folderService.MoveAsync(id, request, ct);
            return TypedResults.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
