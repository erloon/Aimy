using Aimy.API.Models;
using FluentValidation;

namespace Aimy.API.Validators;

public class MetadataValueOptionUpsertRequestValidator : AbstractValidator<MetadataValueOptionUpsertRequest>
{
    public MetadataValueOptionUpsertRequestValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty()
            .WithMessage("Key is required.")
            .MaximumLength(128)
            .WithMessage("Key must not exceed 128 characters.");

        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Value is required.")
            .MaximumLength(256)
            .WithMessage("Value must not exceed 256 characters.");

        RuleFor(x => x.Label)
            .NotEmpty()
            .WithMessage("Label is required.")
            .MaximumLength(256)
            .WithMessage("Label must not exceed 256 characters.");

        RuleFor(x => x.Aliases)
            .NotNull()
            .WithMessage("Aliases must not be null.");

        RuleForEach(x => x.Aliases)
            .MaximumLength(256)
            .WithMessage("Each alias must not exceed 256 characters.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("SortOrder must be zero or positive.");
    }
}