using Aimy.API.Models;
using Aimy.Core.Application.Interfaces.Upload;
using Aimy.Core.Domain.Entities;
using FluentValidation;
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
            .Produces<MetadataKeysResponse>(StatusCodes.Status200OK)
            .Produces<UnauthorizedHttpResult>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/values", GetValues)
            .WithName("GetMetadataValues")
            .WithSummary("List metadata value suggestions by key")
            .Produces<MetadataValuesResponse>(StatusCodes.Status200OK)
            .Produces<BadRequest<ErrorResponse>>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedHttpResult>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/normalize", Normalize)
            .WithName("NormalizeMetadata")
            .WithSummary("Normalize metadata payload")
            .Accepts<MetadataNormalizeRequest>("application/json")
            .Produces<BadRequest<ErrorResponse>>(StatusCodes.Status400BadRequest)
            .Produces<MetadataNormalizeResponse>(StatusCodes.Status200OK)
            .Produces<UnauthorizedHttpResult>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        adminGroup.MapPost("/definitions", UpsertDefinition)
            .WithName("UpsertMetadataDefinition")
            .WithSummary("Create or update metadata definition")
            .Accepts<MetadataDefinitionUpsertRequest>("application/json")
            .Produces<MetadataDefinitionResponse>(StatusCodes.Status200OK)
            .Produces<BadRequest<ErrorResponse>>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedHttpResult>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        adminGroup.MapPost("/values", UpsertValueOption)
            .WithName("UpsertMetadataValueOption")
            .WithSummary("Create or update metadata value option")
            .Accepts<MetadataValueOptionUpsertRequest>("application/json")
            .Produces<MetadataValueOptionResponse>(StatusCodes.Status200OK)
            .Produces<BadRequest<ErrorResponse>>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedHttpResult>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<Results<Ok<MetadataKeysResponse>, UnauthorizedHttpResult, ProblemHttpResult>> GetKeys(
        IUploadKnowledgeSyncService service,
        CancellationToken ct)
    {
        try
        {
            var definitions = await service.GetMetadataDefinitionsAsync(ct);
            var payload = new MetadataKeysResponse
            {
                Items = definitions.Select(definition => new MetadataKeyResponseItem
                {
                    Key = definition.Key,
                    Label = definition.Label,
                    Type = definition.ValueType,
                    Filterable = definition.Filterable,
                    AllowFreeText = definition.AllowFreeText,
                    Required = definition.Required,
                    Policy = definition.Policy.ToString()
                }).ToList()
            };

            return TypedResults.Ok(payload);
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

    private static async Task<Results<Ok<MetadataValuesResponse>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, ProblemHttpResult>> GetValues(
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
            var payload = new MetadataValuesResponse
            {
                Key = suggestions.Key,
                Items = suggestions.Items.Select(item => new MetadataValueSuggestionResponseItem
                {
                    Value = item.Value,
                    Label = item.Label,
                    Aliases = item.Aliases,
                    MatchType = item.MatchType
                }).ToList()
            };

            return TypedResults.Ok(payload);
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

    private static async Task<Results<Ok<MetadataNormalizeResponse>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, ProblemHttpResult>> Normalize(
        MetadataNormalizeRequest request,
        IValidator<MetadataNormalizeRequest> validator,
        IUploadKnowledgeSyncService service,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage));
            return TypedResults.BadRequest(new ErrorResponse { Error = errors });
        }

        try
        {
            var result = await service.NormalizeMetadataAsync(request.Metadata, request.DefaultPolicy, ct);
            var payload = new MetadataNormalizeResponse
            {
                Metadata = result.NormalizedMetadata,
                HasChanges = result.HasChanges,
                Warnings = result.Warnings.Select(warning => new MetadataNormalizeWarningResponse
                {
                    Key = warning.Key,
                    Message = warning.Message,
                    InputValue = warning.InputValue,
                    ResolvedValue = warning.ResolvedValue,
                    MatchType = warning.MatchType.ToString()
                }).ToList()
            };

            return TypedResults.Ok(payload);
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

    private static async Task<Results<Ok<MetadataDefinitionResponse>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, ProblemHttpResult>> UpsertDefinition(
        MetadataDefinitionUpsertRequest request,
        IValidator<MetadataDefinitionUpsertRequest> validator,
        IUploadKnowledgeSyncService service,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage));
            return TypedResults.BadRequest(new ErrorResponse { Error = errors });
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

            var payload = new MetadataDefinitionResponse
            {
                Id = definition.Id,
                Key = definition.Key,
                Label = definition.Label,
                Type = definition.ValueType,
                Filterable = definition.Filterable,
                AllowFreeText = definition.AllowFreeText,
                Required = definition.Required,
                Policy = definition.Policy.ToString(),
                IsActive = definition.IsActive
            };

            return TypedResults.Ok(payload);
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

    private static async Task<Results<Ok<MetadataValueOptionResponse>, BadRequest<ErrorResponse>, UnauthorizedHttpResult, ProblemHttpResult>> UpsertValueOption(
        MetadataValueOptionUpsertRequest request,
        IValidator<MetadataValueOptionUpsertRequest> validator,
        IUploadKnowledgeSyncService service,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage));
            return TypedResults.BadRequest(new ErrorResponse { Error = errors });
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

            var payload = new MetadataValueOptionResponse
            {
                Id = option.Id,
                Value = option.CanonicalValue,
                Label = option.DisplayLabel,
                Aliases = option.Aliases,
                IsActive = option.IsActive,
                SortOrder = option.SortOrder
            };

            return TypedResults.Ok(payload);
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
