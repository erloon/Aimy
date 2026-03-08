using Aimy.Core.Domain.Entities;
using FluentAssertions;

namespace Aimy.Tests.Domain.Entities;

[TestFixture]
public class UploadTests
{
    [TestCase("text/markdown")]
    [TestCase("text/x-markdown")]
    [TestCase("application/markdown")]
    [TestCase("application/x-markdown")]
    [TestCase("text/markdown; charset=utf-8")]
    [TestCase("TEXT/MARKDOWN")]
    public void IsMarkdownUpload_WhenContentTypeIsMarkdown_ReturnsTrue(string contentType)
    {
        // Arrange
        var upload = CreateUpload("document.pdf", contentType);

        // Act
        var result = upload.IsMarkdownUpload;

        // Assert
        result.Should().BeTrue();
    }

    [TestCase("note.md")]
    [TestCase("note.markdown")]
    [TestCase("note.mdown")]
    [TestCase("note.mkd")]
    [TestCase("NOTE.MD")]
    public void IsMarkdownUpload_WhenExtensionIsMarkdown_ReturnsTrue(string fileName)
    {
        // Arrange
        var upload = CreateUpload(fileName, "application/pdf");

        // Act
        var result = upload.IsMarkdownUpload;

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsMarkdownUpload_WhenNoMarkdownSignals_ReturnsFalse()
    {
        // Arrange
        var upload = CreateUpload("document.pdf", "application/pdf");

        // Act
        var result = upload.IsMarkdownUpload;

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsMarkdownUpload_WhenContentTypeMissingAndExtensionNotMarkdown_ReturnsFalse()
    {
        // Arrange
        var upload = CreateUpload("document.txt", null);

        // Act
        var result = upload.IsMarkdownUpload;

        // Assert
        result.Should().BeFalse();
    }

    private static Upload CreateUpload(string fileName, string? contentType)
    {
        return new Upload
        {
            UserId = Guid.NewGuid(),
            FileName = fileName,
            StoragePath = $"uploads/{fileName}",
            ContentType = contentType
        };
    }
}
