using Aimy.API.Models;
using Aimy.Core.Application.DTOs;
using Aimy.Core.Application.DTOs.KnowledgeBase;
using Aimy.Core.Domain.Entities;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Aimy.API.Endpoints;

public static class KnowledgeItemEndpoints
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB
    private static readonly string[] AllowedExtensions = [".txt", ".docx", ".md", ".pdf"];

    public static IEndpointRouteBuilder MapKnowledgeItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/kb/items")
            .WithTags("Knowledge Base - Items")
            .RequireAuthorization();

        group.MapGet("/", SearchItems)
            .WithName("SearchItems")
            .WithSummary("Search knowledge items with pagination and filters")
            .Produces<PagedResult<ItemResponse>>()
            .Produces<UnauthorizedHttpResult>()
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id}", GetItem)
            .WithName("GetItem")
            .WithSummary("Get a knowledge item by ID")
            .Produces<ItemResponse>()
            .Produces<UnauthorizedHttpResult>()
            .Produces<NotFound>()
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/simple-semantic-search", SimpleSemanticSearch)
            .WithName("Simple Semantic Search")
            .WithSummary("Search knowledge items")
            .Produces<PagedResult<SemanticSearchResultResponse>>()
            .Produces<UnauthorizedHttpResult>()
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/note", CreateNote)
            .WithName("CreateNote")
            .WithSummary("Create a new note (auto-creates markdown upload)")
            .Accepts<CreateNoteRequest>("application/json")
            .Produces<ItemResponse>(StatusCodes.Status201Created)
            .Produces<BadRequest<ErrorResponse>>()
            .Produces<UnauthorizedHttpResult>()
            .Produces<NotFound>()
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/from-upload", CreateFromUpload)
            .WithName("CreateItemFromUpload")
            .WithSummary("Create a knowledge item from an existing upload")
            .Accepts<CreateItemFromUploadRequest>("application/json")
            .Produces<ItemResponse>(StatusCodes.Status201Created)
            .Produces<BadRequest<ErrorResponse>>()
            .Produces<UnauthorizedHttpResult>()
            .Produces<NotFound>()
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/upload", UploadToFolder)
            .WithName("UploadToFolder")
            .WithSummary("Upload a file and create a knowledge item in one step")
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ItemResponse>(StatusCodes.Status201Created)
            .Produces<BadRequest<ErrorResponse>>()
            .Produces<UnauthorizedHttpResult>()
            .Produces<NotFound>()
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPut("/{id}", UpdateItem)
            .WithName("UpdateItem")
            .WithSummary("Update a knowledge item")
            .Accepts<UpdateItemRequest>("application/json")
            .Produces<ItemResponse>()
            .Produces<BadRequest<ErrorResponse>>()
            .Produces<UnauthorizedHttpResult>()
            .Produces<NotFound>()
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{id}", DeleteItem)
            .WithName("DeleteItem")
            .WithSummary("Delete a knowledge item (note: does not delete underlying upload)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<UnauthorizedHttpResult>()
            .Produces<NotFound>()
            .Produces(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<Results<Ok<PagedResult<ItemResponse>>, BadRequest<ErrorResponse>, UnauthorizedHttpResult,
        ProblemHttpResult>> SearchItems(
        IKnowledgeItemService itemService,
        Guid? folderId,
        bool includeSubFolders,
        string? search,
        string? metadata,
        KnowledgeItemType? type,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Page must be at least 1" });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "PageSize must be between 1 and 100" });
        }

        try
        {
            var request = new ItemSearchRequest
            {
                FolderId = folderId,
                IncludeSubFolders = includeSubFolders,
                Search = search,
                Metadata = metadata,
                Type = type,
                Page = page,
                PageSize = pageSize
            };

            var result = await itemService.SearchAsync(request, ct);
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

    private static async
        Task<Results<Ok<PagedResult<SemanticSearchResultResponse>>, BadRequest<ErrorResponse>, UnauthorizedHttpResult,
            ProblemHttpResult>> SimpleSemanticSearch(
            ISemanticSearchService semanticSearchService,
            string query,
            int page = 1,
            int pageSize = 10,
            CancellationToken ct = default)
    {
        if (page < 1)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Page must be at least 1" });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "PageSize must be between 1 and 100" });
        }

        try
        {
            var result = await semanticSearchService.SearchAsync(query, page, pageSize, ct);
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

    private static async Task<Results<Ok<ItemResponse>, UnauthorizedHttpResult, NotFound, ProblemHttpResult>> GetItem(
        Guid id,
        IKnowledgeItemService itemService,
        CancellationToken ct)
    {
        try
        {
            var result = await itemService.GetByIdAsync(id, ct);
            if (result is null)
            {
                return TypedResults.NotFound();
            }

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

    private static async Task<Results<Created<ItemResponse>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, NotFound
        , ProblemHttpResult>> CreateNote(
        CreateNoteRequest request,
        IKnowledgeItemService itemService,
        CancellationToken ct)
    {
        try
        {
            var result = await itemService.CreateNoteAsync(request, ct);
            return TypedResults.Created($"/kb/items/{result.Id}", result);
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

    private static async
        Task<Results<Created<ItemResponse>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, NotFound,
            ProblemHttpResult>> CreateFromUpload(
            CreateItemFromUploadRequest request,
            IKnowledgeItemService itemService,
            CancellationToken ct)
    {
        try
        {
            var result = await itemService.CreateFromUploadAsync(request, ct);
            return TypedResults.Created($"/kb/items/{result.Id}", result);
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

    private static async Task<Results<Created<ItemResponse>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, NotFound,
        ProblemHttpResult>> UploadToFolder(
        [FromForm] IFormFile file,
        [FromForm] Guid folderId,
        [FromForm] string? title,
        [FromForm] string? metadata,
        IKnowledgeItemService itemService,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "File is required" });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "File size must not exceed 50MB" });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return TypedResults.BadRequest(new ErrorResponse
            {
                Error = $"File extension must be one of: {string.Join(", ", AllowedExtensions)}"
            });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var request = new UploadToFolderRequest
            {
                FolderId = folderId,
                Title = title,
                Metadata = metadata,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileStream = stream
            };

            var result = await itemService.UploadToFolderAsync(request, ct);
            return TypedResults.Created($"/kb/items/{result.Id}", result);
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

    private static async Task<Results<Ok<ItemResponse>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, NotFound,
        ProblemHttpResult>> UpdateItem(
        Guid id,
        UpdateItemRequest request,
        IKnowledgeItemService itemService,
        CancellationToken ct)
    {
        try
        {
            var result = await itemService.UpdateAsync(id, request, ct);
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

    private static async Task<Results<NoContent, UnauthorizedHttpResult, NotFound, ProblemHttpResult>> DeleteItem(
        Guid id,
        IKnowledgeItemService itemService,
        CancellationToken ct)
    {
        try
        {
            await itemService.DeleteAsync(id, ct);
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
        catch (Exception ex)
        {
            return TypedResults.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
