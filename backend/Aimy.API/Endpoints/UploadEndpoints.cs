using Aimy.API.Validators;
using Aimy.Core.Application.Interfaces;

namespace Aimy.API.Endpoints;

public static class UploadEndpoints
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB
    private static readonly string[] AllowedExtensions = [".txt", ".docx", ".md", ".pdf"];

    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/upload")
            .WithTags("Upload")
            .RequireAuthorization();

        group.MapPost("/", UploadFile)
            .WithName("UploadFile")
            .DisableAntiforgery()
            .WithDescription("Upload a file to storage");

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
}
