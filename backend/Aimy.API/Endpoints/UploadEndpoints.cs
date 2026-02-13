using Aimy.API.Validators;
using Aimy.Core.Application.Interfaces;

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
            .DisableAntiforgery()
            .WithDescription("Upload a file to storage");

        var uploadsGroup = app.MapGroup("/uploads")
            .WithTags("Uploads")
            .RequireAuthorization();

        uploadsGroup.MapGet("/", ListFiles)
            .WithName("ListFiles")
            .WithDescription("List uploaded files with pagination");

        uploadsGroup.MapGet("/{id}/download", DownloadFile)
            .WithName("DownloadFile")
            .WithDescription("Download a file by ID");

        uploadsGroup.MapDelete("/{id}", DeleteFile)
            .WithName("DeleteFile")
            .WithDescription("Delete a file by ID");

        uploadsGroup.MapPatch("/{id}/metadata", UpdateMetadata)
            .WithName("UpdateMetadata")
            .WithDescription("Update file metadata");

        return app;
    }

    private static async Task<IResult> UploadFile(
        IFormFile file,
        IUploadService uploadService,
        string? metadata,
        CancellationToken ct)
    {
        // Validate file is present
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { error = "File is required" });
        }

        // Validate file size
        if (file.Length > MaxFileSizeBytes)
        {
            return Results.BadRequest(new { error = "File size must not exceed 50MB" });
        }

        // Validate file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return Results.BadRequest(new { error = $"File extension must be one of: {string.Join(", ", AllowedExtensions)}" });
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

            return Results.Created($"/upload/{response.Id}", response);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Upload failed");
        }
    }

    /// <summary>
    /// Lists uploaded files with pagination support.
    /// </summary>
    private static async Task<IResult> ListFiles(
        IUploadService uploadService,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        // Validate pagination parameters
        if (page < 1)
        {
            return Results.BadRequest(new { error = "Page must be at least 1" });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Results.BadRequest(new { error = "PageSize must be between 1 and 100" });
        }

        try
        {
            var result = await uploadService.ListAsync(page, pageSize, ct);
            return Results.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to list files");
        }
    }

    /// <summary>
    /// Downloads a file by its ID.
    /// </summary>
    private static async Task<IResult> DownloadFile(
        Guid id,
        IUploadService uploadService,
        CancellationToken ct)
    {
        try
        {
            var stream = await uploadService.DownloadAsync(id, ct);
            return Results.File(stream, "application/octet-stream");
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to download file");
        }
    }

    /// <summary>
    /// Deletes a file by its ID.
    /// </summary>
    private static async Task<IResult> DeleteFile(
        Guid id,
        IUploadService uploadService,
        CancellationToken ct)
    {
        try
        {
            await uploadService.DeleteAsync(id, ct);
            return Results.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to delete file");
        }
    }

    /// <summary>
    /// Updates the metadata of a file.
    /// </summary>
    private static async Task<IResult> UpdateMetadata(
        Guid id,
        IUploadService uploadService,
        string? metadata,
        CancellationToken ct)
    {
        try
        {
            var result = await uploadService.UpdateMetadataAsync(id, metadata, ct);
            return Results.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to update metadata");
        }
    }
}
