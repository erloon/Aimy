using Aimy.Core.Domain.Entities;
using FluentAssertions;

namespace Aimy.Tests.Domain.Entities;

[TestFixture]
public class UploadTests
{
    [Test]
    public void Upload_Constructor_SetsDefaultValues()
    {
        // Act
        var upload = new Upload
        {
            UserId = Guid.NewGuid(),
            FileName = "document.pdf",
            StoragePath = "uploads/document.pdf"
        };

        // Assert
        upload.Id.Should().NotBeEmpty();
        upload.DateUploaded.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
