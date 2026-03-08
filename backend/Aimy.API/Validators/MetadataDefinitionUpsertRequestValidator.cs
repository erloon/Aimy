using Aimy.API.Models;
using FluentValidation;

namespace Aimy.API.Validators;

public class MetadataDefinitionUpsertRequestValidator : AbstractValidator<MetadataDefinitionUpsertRequest>
{
    public MetadataDefinitionUpsertRequestValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty()
            .WithMessage("Key is required.")
            .MaximumLength(128)
            .WithMessage("Key must not exceed 128 characters.")
            .Matches(@"^[a-zA-Z0-9_\-]+$")
            .WithMessage("Key must contain only alphanumeric characters, hyphens and underscores.");

        RuleFor(x => x.Label)
            .NotEmpty()
            .WithMessage("Label is required.")
            .MaximumLength(256)
            .WithMessage("Label must not exceed 256 characters.");

        RuleFor(x => x.ValueType)
            .NotEmpty()
            .WithMessage("ValueType is required.")
            .MaximumLength(64)
            .WithMessage("ValueType must not exceed 64 characters.");

        RuleFor(x => x.Policy)
            .IsInEnum()
            .WithMessage("Policy must be a valid MetadataNormalizationPolicy value.");
    }
}