using Aimy.API.Models;
using Aimy.Core.Application.DTOs;
using Aimy.Core.Application.DTOs.Upload;
using Aimy.Core.Application.Interfaces.Upload;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Aimy.API.Endpoints;

public static class UploadEndpoints
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB
    private static readonly string[] AllowedExtensions = [".txt", ".docx", ".md", ".pdf"];

    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        var uploadGroup = app.MapGroup("/upload")
            .WithTags("Upload")
            .RequireAuthorization();

        uploadGroup.MapPost("/", UploadFile)
            .WithName("UploadFile")
            .WithSummary("Upload a file to storage")
            .WithDescription("Uploads a file to the storage system. Supports files up to 50MB with extensions: .txt, .docx, .md, .pdf. Optionally accepts metadata as a JSON string.")
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<UploadFileResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        var uploadsGroup = app.MapGroup("/uploads")
            .WithTags("Uploads")
            .RequireAuthorization();

        uploadsGroup.MapGet("/", ListFiles)
            .WithName("ListFiles")
            .WithSummary("List uploaded files with pagination")
            .WithDescription("Retrieves a paginated list of all uploaded files. Returns file metadata including ID, name, size, upload date, and download link.")
            .Produces<PagedResult<UploadFileResponse>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        uploadsGroup.MapGet("/{id}/download", DownloadFile)
            .WithName("DownloadFile")
            .WithSummary("Download a file by ID")
            .WithDescription("Downloads a file from storage using its unique identifier. Returns the file as a binary stream.")
            .Produces<FileStreamHttpResult>(StatusCodes.Status200OK, "application/octet-stream")
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        uploadsGroup.MapDelete("/{id}", DeleteFile)
            .WithName("DeleteFile")
            .WithSummary("Delete a file by ID")
            .WithDescription("Permanently deletes a file from storage using its unique identifier.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        uploadsGroup.MapPatch("/{id}/metadata", UpdateMetadata)
            .WithName("UpdateMetadata")
            .WithSummary("Update file metadata")
            .WithDescription("Updates the metadata of an existing file. Metadata should be provided as a JSON object containing key-value pairs.")
            .Accepts<UpdateMetadataRequest>("application/json")
            .Produces<UploadFileResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        return app;
    }


    /// <summary>
    /// Uploads a file to the storage system
    /// </summary>
    /// <param name="file">The file to upload (multipart/form-data)</param>
    /// <param name="uploadService">Upload service for file operations</param>
    /// <param name="metadata">Optional JSON string containing file metadata</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Upload response with file details and download link</returns>
    /// <response code="201">File uploaded successfully</response>
    /// <response code="400">Invalid file (size, extension, or missing file)</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="500">Internal server error during upload</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /upload
    ///     Content-Type: multipart/form-data
    ///     
    ///     file: [binary file data]
    ///     metadata: {"category": "documents", "tags": ["important"]}
    /// 
    /// Constraints:
    /// - Maximum file size: 50MB
    /// - Allowed extensions: .txt, .docx, .md, .pdf
    /// </remarks>
    private static async Task<Results<Created<UploadFileResponse>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, ProblemHttpResult>> UploadFile(
        IFormFile file,
        IUploadService uploadService,
        string? metadata,
        CancellationToken ct)
    {
        // Validate file is present
        if (file is null || file.Length == 0)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "File is required" });
        }

        // Validate file size
        if (file.Length > MaxFileSizeBytes)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "File size must not exceed 50MB" });
        }

        // Validate file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = $"File extension must be one of: {string.Join(", ", AllowedExtensions)}" });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var response = await uploadService.UploadAsync(
                stream,
                file.FileName,
                file.ContentType,
                metadata,
                ct);

            return TypedResults.Created($"/upload/{response.Id}", response);
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
                title: "Upload failed");
        }
    }


    /// <summary>
    /// Lists uploaded files with pagination support
    /// </summary>
    /// <param name="uploadService">Upload service for file operations</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Number of items per page (1-100, default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of uploaded files</returns>
    /// <response code="200">Returns paginated list of files</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /uploads?page=1&amp;pageSize=10
    /// 
    /// Sample response:
    /// 
    ///     {
    ///       "items": [...],
    ///       "page": 1,
    ///       "pageSize": 10,
    ///       "totalCount": 42,
    ///       "totalPages": 5
    ///     }
    /// </remarks>
    private static async Task<Results<Ok<PagedResult<UploadFileResponse>>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, ProblemHttpResult>> ListFiles(
        IUploadService uploadService,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        // Validate pagination parameters
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
            var result = await uploadService.ListAsync(page, pageSize, ct);
            return TypedResults.Ok(result);
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
                title: "Failed to list files");
        }
    }


    /// <summary>
    /// Downloads a file by its unique identifier
    /// </summary>
    /// <param name="id">Unique identifier of the file to download</param>
    /// <param name="uploadService">Upload service for file operations</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File stream for download</returns>
    /// <response code="200">Returns the file as a binary stream</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">File not found</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /uploads/3fa85f64-5717-4562-b3fc-2c963f66afa6/download
    /// </remarks>
    private static async Task<Results<FileStreamHttpResult, UnauthorizedHttpResult, NotFound, ProblemHttpResult>> DownloadFile(
        Guid id,
        IUploadService uploadService,
        CancellationToken ct)
    {
        try
        {
            var stream = await uploadService.DownloadAsync(id, ct);
            return TypedResults.File(stream, "application/octet-stream");
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
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to download file");
        }
    }


    /// <summary>
    /// Permanently deletes a file from storage
    /// </summary>
    /// <param name="id">Unique identifier of the file to delete</param>
    /// <param name="uploadService">Upload service for file operations</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">File deleted successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">File not found</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     DELETE /uploads/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// </remarks>
    private static async Task<Results<NoContent, BadRequest<ErrorResponse>, UnauthorizedHttpResult, NotFound, ProblemHttpResult>> DeleteFile(
        Guid id,
        IUploadService uploadService,
        CancellationToken ct)
    {
        try
        {
            await uploadService.DeleteAsync(id, ct);
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
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to delete file");
        }
    }


    /// <summary>
    /// Updates the metadata of an existing file
    /// </summary>
    /// <param name="id">Unique identifier of the file</param>
    /// <param name="request">Request model containing metadata JSON</param>
    /// <param name="uploadService">Upload service for file operations</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated file information</returns>
    /// <response code="200">Metadata updated successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">File not found</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PATCH /uploads/3fa85f64-5717-4562-b3fc-2c963f66afa6/metadata
    ///     Content-Type: application/json
    ///     
    ///     {
    ///       "metadata": "{\"category\": \"documents\", \"tags\": [\"important\", \"2024\"]}"
    ///     }
    /// </remarks>
    private static async Task<Results<Ok<UploadFileResponse>, UnauthorizedHttpResult, NotFound, ProblemHttpResult>> UpdateMetadata(
        Guid id,
        UpdateMetadataRequest request,
        IUploadService uploadService,
        CancellationToken ct)
    {
        try
        {
            var result = await uploadService.UpdateMetadataAsync(id, request.Metadata, ct);
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
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to update metadata");
        }
    }
}
