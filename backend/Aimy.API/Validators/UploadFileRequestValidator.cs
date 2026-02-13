using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Aimy.API.Validators;

public class UploadFileRequestValidator : AbstractValidator<IFormFile>
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB
    private static readonly string[] AllowedExtensions = [".txt", ".docx", ".md", ".pdf"];

    public UploadFileRequestValidator()
    {
        RuleFor(file => file)
            .NotNull()
            .WithMessage("File is required");

        RuleFor(file => file.Length)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"File size must not exceed 50MB");

        RuleFor(file => file.FileName)
            .Must(HaveValidExtension)
            .WithMessage($"File extension must be one of: {string.Join(", ", AllowedExtensions)}");
    }

    private static bool HaveValidExtension(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }
}
