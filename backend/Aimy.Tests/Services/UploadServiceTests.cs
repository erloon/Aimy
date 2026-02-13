using Aimy.Core.Application.Interfaces;
using Aimy.Core.Application.Services;
using Aimy.Core.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Aimy.Tests.Services;

[TestFixture]
public class UploadServiceTests
{
    private Mock<IStorageService> _storageServiceMock = null!;
    private Mock<IUploadRepository> _uploadRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private UploadService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _storageServiceMock = new Mock<IStorageService>();
        _uploadRepositoryMock = new Mock<IUploadRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _sut = new UploadService(
            _storageServiceMock.Object,
            _uploadRepositoryMock.Object,
            _currentUserServiceMock.Object);
    }

    [Test]
    public async Task UploadAsync_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();
        var fileName = "test.pdf";
        var contentType = "application/pdf";
        var metadata = "{\"category\": \"invoice\"}";
        var storagePath = $"uploads/{userId}/{fileName}";
        var fileSizeBytes = 1024L;

        using var fileStream = new MemoryStream(new byte[fileSizeBytes]);

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _uploadRepositoryMock
            .Setup(r => r.GetByUserIdAndFileNameAsync(userId, fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _storageServiceMock
            .Setup(s => s.UploadAsync(userId, fileName, fileStream, contentType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storagePath);

        _uploadRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Upload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Upload upload, CancellationToken _) =>
            {
                upload.Id = uploadId;
                return upload;
            });

        // Act
        var result = await _sut.UploadAsync(fileStream, fileName, contentType, metadata, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(uploadId);
        result.FileName.Should().Be(fileName);
        result.Link.Should().Be(storagePath);
        result.SizeBytes.Should().Be(fileSizeBytes);
        result.ContentType.Should().Be(contentType);
        result.Metadata.Should().Be(metadata);
        result.UploadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        _storageServiceMock.Verify(s => s.UploadAsync(userId, fileName, fileStream, contentType, It.IsAny<CancellationToken>()), Times.Once);
        _uploadRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Upload>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UploadAsync_NoCurrentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns((Guid?)null);

        using var fileStream = new MemoryStream(new byte[1024]);

        // Act
        var act = () => _sut.UploadAsync(fileStream, "test.pdf", "application/pdf", null, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not authenticated");

        _storageServiceMock.Verify(s => s.UploadAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _uploadRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Upload>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void UploadAsync_StorageFails_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _uploadRepositoryMock
            .Setup(r => r.GetByUserIdAndFileNameAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _storageServiceMock
            .Setup(s => s.UploadAsync(userId, It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Storage service unavailable"));

        using var fileStream = new MemoryStream(new byte[1024]);

        // Act
        var act = () => _sut.UploadAsync(fileStream, "test.pdf", "application/pdf", null, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Storage service unavailable");

        _uploadRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Upload>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UploadAsync_DuplicateFileName_AppendsSuffix()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();
        var originalFileName = "test.pdf";
        var expectedFileName = "test (1).pdf";
        var storagePath = $"uploads/{userId}/{expectedFileName}";
        var fileSizeBytes = 1024L;

        using var fileStream = new MemoryStream(new byte[fileSizeBytes]);

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        // First call returns existing file (duplicate), second call returns empty (no duplicate with new name)
        _uploadRepositoryMock
            .SetupSequence(r => r.GetByUserIdAndFileNameAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Upload { FileName = originalFileName, StoragePath = "existing/path" }])
            .ReturnsAsync([]);

        _storageServiceMock
            .Setup(s => s.UploadAsync(userId, expectedFileName, fileStream, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storagePath);

        _uploadRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Upload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Upload upload, CancellationToken _) =>
            {
                upload.Id = uploadId;
                return upload;
            });

        // Act
        var result = await _sut.UploadAsync(fileStream, originalFileName, "application/pdf", null, CancellationToken.None);

        // Assert
        result.FileName.Should().Be(expectedFileName);
    }
}
