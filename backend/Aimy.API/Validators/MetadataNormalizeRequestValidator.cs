using System.Text.Json;
using Aimy.API.Models;
using FluentValidation;

namespace Aimy.API.Validators;

public class MetadataNormalizeRequestValidator : AbstractValidator<MetadataNormalizeRequest>
{
    public MetadataNormalizeRequestValidator()
    {
        RuleFor(x => x.Metadata)
            .Must(BeValidJsonOrNull)
            .WithMessage("Metadata must be valid JSON when provided.");

        RuleFor(x => x.DefaultPolicy)
            .IsInEnum()
            .WithMessage("DefaultPolicy must be a valid MetadataNormalizationPolicy value.");
    }

    private static bool BeValidJsonOrNull(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return true;

        try
        {
            JsonDocument.Parse(metadata);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}