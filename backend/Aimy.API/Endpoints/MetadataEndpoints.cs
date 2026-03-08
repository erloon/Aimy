using Aimy.API.Models;
using Aimy.Core.Application.Interfaces.Upload;
using Aimy.Core.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Aimy.API.Endpoints;

public static class MetadataEndpoints
{
    public static IEndpointRouteBuilder MapMetadataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/kb/metadata")
            .WithTags("Knowledge Base - Metadata")
            .RequireAuthorization();

        var adminGroup = app.MapGroup("/kb/metadata")
            .WithTags("Knowledge Base - Metadata")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/keys", GetKeys)
            .WithName("GetMetadataKeys")
            .WithSummary("List metadata key definitions")
            .Produces(StatusCodes.Status200OK)
            .Produces<UnauthorizedHttpResult>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/values", GetValues)
            .WithName("GetMetadataValues")
            .WithSummary("List metadata value suggestions by key")
            .Produces(StatusCodes.Status200OK)
            .Produces<BadRequest<ErrorResponse>>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedHttpResult>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/normalize", Normalize)
            .WithName("NormalizeMetadata")
            .WithSummary("Normalize metadata payload")
            .Accepts<MetadataNormalizeRequest>("application/json")
            .Produces(StatusCodes.Status200OK)
            .Produces<UnauthorizedHttpResult>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        adminGroup.MapPost("/definitions", UpsertDefinition)
            .WithName("UpsertMetadataDefinition")
            .WithSummary("Create or update metadata definition")
            .Accepts<MetadataDefinitionUpsertRequest>("application/json")
            .Produces(StatusCodes.Status200OK)
            .Produces<BadRequest<ErrorResponse>>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedHttpResult>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        adminGroup.MapPost("/values", UpsertValueOption)
            .WithName("UpsertMetadataValueOption")
            .WithSummary("Create or update metadata value option")
            .Accepts<MetadataValueOptionUpsertRequest>("application/json")
            .Produces(StatusCodes.Status200OK)
            .Produces<BadRequest<ErrorResponse>>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedHttpResult>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<Results<Ok<object>, UnauthorizedHttpResult, ProblemHttpResult>> GetKeys(
        IUploadKnowledgeSyncService service,
        CancellationToken ct)
    {
        try
        {
            var definitions = await service.GetMetadataDefinitionsAsync(ct);
            var payload = definitions.Select(definition => new
            {
                key = definition.Key,
                label = definition.Label,
                type = definition.ValueType,
                filterable = definition.Filterable,
                allowFreeText = definition.AllowFreeText,
                required = definition.Required,
                policy = definition.Policy.ToString()
            });

            return TypedResults.Ok<object>(new { items = payload });
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
                title: "Failed to retrieve metadata keys");
        }
    }

    private static async Task<Results<Ok<object>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, ProblemHttpResult>> GetValues(
        string? key,
        string? q,
        IUploadKnowledgeSyncService service,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Query parameter 'key' is required." });
        }

        try
        {
            var suggestions = await service.GetMetadataValueSuggestionsAsync(key, q, ct);
            var payload = new
            {
                key = suggestions.Key,
                items = suggestions.Items.Select(item => new
                {
                    value = item.Value,
                    label = item.Label,
                    aliases = item.Aliases,
                    matchType = item.MatchType
                })
            };

            return TypedResults.Ok<object>(payload);
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
                title: "Failed to retrieve metadata values");
        }
    }

    private static async Task<Results<Ok<object>, UnauthorizedHttpResult, ProblemHttpResult>> Normalize(
        MetadataNormalizeRequest request,
        IUploadKnowledgeSyncService service,
        CancellationToken ct)
    {
        try
        {
            var result = await service.NormalizeMetadataAsync(request.Metadata, request.DefaultPolicy, ct);
            return TypedResults.Ok<object>(new
            {
                metadata = result.NormalizedMetadata,
                hasChanges = result.HasChanges,
                warnings = result.Warnings.Select(warning => new
                {
                    key = warning.Key,
                    message = warning.Message,
                    inputValue = warning.InputValue,
                    resolvedValue = warning.ResolvedValue,
                    matchType = warning.MatchType.ToString()
                })
            });
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
                title: "Failed to normalize metadata");
        }
    }

    private static async Task<Results<Ok<object>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, ProblemHttpResult>> UpsertDefinition(
        MetadataDefinitionUpsertRequest request,
        IUploadKnowledgeSyncService service,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Key) || string.IsNullOrWhiteSpace(request.Label))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Key and Label are required." });
        }

        try
        {
            var definition = await service.UpsertDefinitionAsync(new MetadataDefinition
            {
                Key = request.Key.Trim(),
                Label = request.Label.Trim(),
                ValueType = request.ValueType.Trim(),
                Filterable = request.Filterable,
                AllowFreeText = request.AllowFreeText,
                Required = request.Required,
                Policy = request.Policy,
                IsActive = request.IsActive,
                UpdatedAt = DateTime.UtcNow
            }, ct);

            return TypedResults.Ok<object>(new
            {
                id = definition.Id,
                key = definition.Key,
                label = definition.Label,
                type = definition.ValueType,
                filterable = definition.Filterable,
                allowFreeText = definition.AllowFreeText,
                required = definition.Required,
                policy = definition.Policy.ToString(),
                isActive = definition.IsActive
            });
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
                title: "Failed to upsert metadata definition");
        }
    }

    private static async Task<Results<Ok<object>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, ProblemHttpResult>> UpsertValueOption(
        MetadataValueOptionUpsertRequest request,
        IUploadKnowledgeSyncService service,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Key)
            || string.IsNullOrWhiteSpace(request.Value)
            || string.IsNullOrWhiteSpace(request.Label))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Key, Value and Label are required." });
        }

        try
        {
            var option = await service.UpsertValueOptionAsync(request.Key.Trim(), new MetadataValueOption
            {
                CanonicalValue = request.Value.Trim(),
                DisplayLabel = request.Label.Trim(),
                Aliases = request.Aliases,
                IsActive = request.IsActive,
                SortOrder = request.SortOrder,
                UpdatedAt = DateTime.UtcNow
            }, ct);

            return TypedResults.Ok<object>(new
            {
                id = option.Id,
                value = option.CanonicalValue,
                label = option.DisplayLabel,
                aliases = option.Aliases,
                isActive = option.IsActive,
                sortOrder = option.SortOrder
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (KeyNotFoundException ex)
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
                title: "Failed to upsert metadata value option");
        }
    }
}
