using Aimy.Core.Application.Interfaces.Ingestion;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Application.Services;
using Aimy.Core.Application.DTOs;
using Aimy.Core.Application.Interfaces.Auth;
using Aimy.Core.Application.Interfaces.Upload;
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
    private Mock<IUploadQueueWriter> _queueWriterMock = null!;
    private Mock<IKnowledgeItemRepository> _knowledgeItemRepositoryMock = null!;
    private Mock<IDataIngestionService> _dataIngestionServiceMock = null!;
    private UploadService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _storageServiceMock = new Mock<IStorageService>();
        _uploadRepositoryMock = new Mock<IUploadRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _queueWriterMock = new Mock<IUploadQueueWriter>();
        _knowledgeItemRepositoryMock = new Mock<IKnowledgeItemRepository>();
        _dataIngestionServiceMock = new Mock<IDataIngestionService>();
        _sut = new UploadService(
            _storageServiceMock.Object,
            _uploadRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _queueWriterMock.Object,
            _knowledgeItemRepositoryMock.Object,
            _dataIngestionServiceMock.Object
            );

        _dataIngestionServiceMock
            .Setup(s => s.GetByUploadIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Aimy.Core.Application.DTOs.Upload.UploadIngestionResponse?)null);

        _knowledgeItemRepositoryMock
            .Setup(r => r.ExistsBySourceUploadIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _knowledgeItemRepositoryMock
            .Setup(r => r.GetBySourceUploadIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _dataIngestionServiceMock
            .Setup(s => s.UpdateMetadataByUploadIdAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
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
        result.Ingestion.Should().BeNull();

        _storageServiceMock.Verify(s => s.UploadAsync(userId, fileName, fileStream, contentType, It.IsAny<CancellationToken>()), Times.Once);
        _uploadRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Upload>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ListAsync_WithIngestion_ReturnsSummaryAndChunks()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();
        var upload = new Upload
        {
            Id = uploadId,
            UserId = userId,
            FileName = "doc.md",
            StoragePath = "uploads/doc.md",
            FileSizeBytes = 120,
            ContentType = "text/markdown",
            DateUploaded = DateTime.UtcNow
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _uploadRepositoryMock
            .Setup(r => r.GetPagedAsync(userId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Upload>
            {
                Items = [upload],
                Page = 1,
                PageSize = 10,
                TotalCount = 1
            });

        _dataIngestionServiceMock
            .Setup(s => s.GetByUploadIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Aimy.Core.Application.DTOs.Upload.UploadIngestionResponse
            {
                Summary = "Main summary",
                Chunks =
                [
                    new Aimy.Core.Application.DTOs.Upload.UploadChunkResponse
                    {
                        Id = Guid.NewGuid(),
                        Content = "chunk content",
                        Context = "chunk context",
                        Summary = "chunk summary",
                        Metadata = "{\"language\":\"en\"}",
                        CreatedAt = DateTime.UtcNow
                    }
                ]
            });

        // Act
        var result = await _sut.ListAsync(1, 10, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var ingestion = result.Items[0].Ingestion;
        ingestion.Should().NotBeNull();
        ingestion!.Summary.Should().Be("Main summary");
        ingestion.Chunks.Should().HaveCount(1);
        ingestion.Chunks[0].Content.Should().Be("chunk content");
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

    [Test]
    public async Task ListAsync_ValidRequest_ReturnsPagedResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var page = 2;
        var pageSize = 5;

        var uploads = new List<Upload>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileName = "a.pdf",
                StoragePath = "uploads/a.pdf",
                FileSizeBytes = 100,
                ContentType = "application/pdf",
                Metadata = "{\"tag\":\"a\"}",
                DateUploaded = DateTime.UtcNow.AddMinutes(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileName = "b.pdf",
                StoragePath = "uploads/b.pdf",
                FileSizeBytes = 200,
                ContentType = "application/pdf",
                Metadata = "{\"tag\":\"b\"}",
                DateUploaded = DateTime.UtcNow.AddMinutes(-1)
            }
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _uploadRepositoryMock
            .Setup(r => r.GetPagedAsync(userId, page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Upload>
            {
                Items = uploads,
                Page = page,
                PageSize = pageSize,
                TotalCount = 12
            });

        // Act
        var result = await _sut.ListAsync(page, pageSize, CancellationToken.None);

        // Assert
        result.Page.Should().Be(page);
        result.PageSize.Should().Be(pageSize);
        result.TotalCount.Should().Be(12);
        result.Items.Should().HaveCount(2);
        result.Items[0].Id.Should().Be(uploads[0].Id);
        result.Items[0].FileName.Should().Be(uploads[0].FileName);
        result.Items[0].Link.Should().Be(uploads[0].StoragePath);
        result.Items[0].Metadata.Should().Be(uploads[0].Metadata);

        _uploadRepositoryMock.Verify(r => r.GetPagedAsync(userId, page, pageSize, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ListAsync_NoCurrentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns((Guid?)null);

        // Act
        var act = () => _sut.ListAsync(1, 10, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not authenticated");

        _uploadRepositoryMock.Verify(r => r.GetPagedAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DownloadAsync_ValidOwnedFile_ReturnsStream()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();
        var storagePath = "uploads/user/file.pdf";
        var expectedStream = new MemoryStream([1, 2, 3]);

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Upload
            {
                Id = uploadId,
                UserId = userId,
                FileName = "file.pdf",
                StoragePath = storagePath
            });

        _storageServiceMock
            .Setup(s => s.DownloadAsync(storagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        // Act
        var result = await _sut.DownloadAsync(uploadId, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedStream);

        _uploadRepositoryMock.Verify(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()), Times.Once);
        _storageServiceMock.Verify(s => s.DownloadAsync(storagePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void DownloadAsync_NoCurrentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var uploadId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns((Guid?)null);

        // Act
        var act = () => _sut.DownloadAsync(uploadId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not authenticated");

        _uploadRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _storageServiceMock.Verify(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DownloadAsync_FileNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Upload?)null);

        // Act
        var act = () => _sut.DownloadAsync(uploadId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("File not found");

        _storageServiceMock.Verify(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DownloadAsync_FileOwnedByDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(currentUserId);

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Upload
            {
                Id = uploadId,
                UserId = ownerId,
                FileName = "file.pdf",
                StoragePath = "uploads/owner/file.pdf"
            });

        // Act
        var act = () => _sut.DownloadAsync(uploadId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User does not have access to this file");

        _storageServiceMock.Verify(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_ValidOwnedFile_DeletesFromStorageAndRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();
        var storagePath = "uploads/user/file.pdf";

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Upload
            {
                Id = uploadId,
                UserId = userId,
                FileName = "file.pdf",
                StoragePath = storagePath
            });

        // Act
        await _sut.DeleteAsync(uploadId, CancellationToken.None);

        // Assert
        _dataIngestionServiceMock.Verify(s => s.DeleteByUploadIdAsync(uploadId, It.IsAny<CancellationToken>()), Times.Once);
        _storageServiceMock.Verify(s => s.DeleteAsync(storagePath, It.IsAny<CancellationToken>()), Times.Once);
        _uploadRepositoryMock.Verify(r => r.DeleteAsync(uploadId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void DeleteAsync_UploadAssignedToKnowledgeBase_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Upload
            {
                Id = uploadId,
                UserId = userId,
                FileName = "file.pdf",
                StoragePath = "uploads/user/file.pdf"
            });

        _knowledgeItemRepositoryMock
            .Setup(r => r.ExistsBySourceUploadIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _sut.DeleteAsync(uploadId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete file assigned to knowledge base");

        _dataIngestionServiceMock.Verify(s => s.DeleteByUploadIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _storageServiceMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _uploadRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteAsync_NoCurrentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var uploadId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns((Guid?)null);

        // Act
        var act = () => _sut.DeleteAsync(uploadId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not authenticated");

        _uploadRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteAsync_FileNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Upload?)null);

        // Act
        var act = () => _sut.DeleteAsync(uploadId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("File not found");

        _storageServiceMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _uploadRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteAsync_FileOwnedByDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(currentUserId);

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Upload
            {
                Id = uploadId,
                UserId = ownerId,
                FileName = "file.pdf",
                StoragePath = "uploads/owner/file.pdf"
            });

        // Act
        var act = () => _sut.DeleteAsync(uploadId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User does not have access to this file");

        _storageServiceMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _uploadRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateMetadataAsync_ValidOwnedFile_UpdatesMetadataAndReturnsResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();
        var newMetadata = "{\"category\":\"invoice\"}";

        var upload = new Upload
        {
            Id = uploadId,
            UserId = userId,
            FileName = "file.pdf",
            StoragePath = "uploads/user/file.pdf",
            FileSizeBytes = 1000,
            ContentType = "application/pdf",
            Metadata = "{\"category\":\"old\"}",
            DateUploaded = DateTime.UtcNow.AddDays(-1)
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(upload);

        // Act
        var result = await _sut.UpdateMetadataAsync(uploadId, newMetadata, CancellationToken.None);

        // Assert
        result.Id.Should().Be(uploadId);
        result.FileName.Should().Be(upload.FileName);
        result.Metadata.Should().Be(newMetadata);
        result.Link.Should().Be(upload.StoragePath);

        _uploadRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Upload>(u => u.Id == uploadId && u.Metadata == newMetadata), It.IsAny<CancellationToken>()), Times.Once);
        _dataIngestionServiceMock.Verify(s => s.UpdateMetadataByUploadIdAsync(uploadId, newMetadata, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateMetadataAsync_WithLinkedKnowledgeItems_UpdatesLinkedItemsMetadata()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();
        var newMetadata = "{\"frameworks\":[\".NET\"]}";

        var upload = new Upload
        {
            Id = uploadId,
            UserId = userId,
            FileName = "file.pdf",
            StoragePath = "uploads/user/file.pdf",
            FileSizeBytes = 1000,
            ContentType = "application/pdf",
            Metadata = "{\"category\":\"old\"}",
            DateUploaded = DateTime.UtcNow.AddDays(-1)
        };

        var linkedItem = new KnowledgeItem
        {
            Id = Guid.NewGuid(),
            FolderId = Guid.NewGuid(),
            Title = "Linked File",
            ItemType = KnowledgeItemType.File,
            SourceUploadId = uploadId,
            Metadata = "{\"category\":\"old\"}"
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(upload);

        _knowledgeItemRepositoryMock
            .Setup(r => r.GetBySourceUploadIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([linkedItem]);

        // Act
        _ = await _sut.UpdateMetadataAsync(uploadId, newMetadata, CancellationToken.None);

        // Assert
        _knowledgeItemRepositoryMock.Verify(
            r => r.UpdateAsync(
                It.Is<KnowledgeItem>(i => i.Id == linkedItem.Id && i.Metadata == newMetadata),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _dataIngestionServiceMock.Verify(s => s.UpdateMetadataByUploadIdAsync(uploadId, newMetadata, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UpdateMetadataAsync_NoCurrentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var uploadId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns((Guid?)null);

        // Act
        var act = () => _sut.UpdateMetadataAsync(uploadId, "{}", CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not authenticated");

        _uploadRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void UpdateMetadataAsync_FileNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Upload?)null);

        // Act
        var act = () => _sut.UpdateMetadataAsync(uploadId, "{}", CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("File not found");

        _uploadRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Upload>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void UpdateMetadataAsync_FileOwnedByDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(currentUserId);

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Upload
            {
                Id = uploadId,
                UserId = ownerId,
                FileName = "file.pdf",
                StoragePath = "uploads/owner/file.pdf"
            });

        // Act
        var act = () => _sut.UpdateMetadataAsync(uploadId, "{}", CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User does not have access to this file");

        _uploadRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Upload>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
