using Aimy.Core.Application.DTOs;
using Aimy.Core.Application.DTOs.KnowledgeBase;
using Aimy.Core.Application.Interfaces;
using Aimy.Core.Application.Interfaces.Auth;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Application.Interfaces.Upload;
using Aimy.Core.Application.Services;
using Aimy.Core.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Aimy.Tests.Services;

[TestFixture]
public class KnowledgeItemServiceTests
{
    private Mock<IKnowledgeBaseRepository> _kbRepositoryMock = null!;
    private Mock<IFolderRepository> _folderRepositoryMock = null!;
    private Mock<IKnowledgeItemRepository> _itemRepositoryMock = null!;
    private Mock<IUploadRepository> _uploadRepositoryMock = null!;
    private Mock<IStorageService> _storageServiceMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private KnowledgeItemService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _kbRepositoryMock = new Mock<IKnowledgeBaseRepository>();
        _folderRepositoryMock = new Mock<IFolderRepository>();
        _itemRepositoryMock = new Mock<IKnowledgeItemRepository>();
        _uploadRepositoryMock = new Mock<IUploadRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _sut = new KnowledgeItemService(
            _kbRepositoryMock.Object,
            _folderRepositoryMock.Object,
            _itemRepositoryMock.Object,
            _uploadRepositoryMock.Object,
            _storageServiceMock.Object,
            _currentUserServiceMock.Object);
    }

    #region CreateNoteAsync Tests

    [Test]
    public async Task CreateNoteAsync_ValidRequest_CreatesNoteWithAutoUpload()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var storagePath = $"notes/{userId}/Test Note.md";
        long? streamPositionDuringUpload = null;

        var request = new CreateNoteRequest
        {
            FolderId = folderId,
            Title = "Test Note",
            Content = "# Test Content",
            Tags = "[\"test\"]"
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Test Folder" });

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _storageServiceMock
            .Setup(s => s.UploadAsync(userId, It.IsAny<string>(), It.IsAny<Stream>(), "text/markdown", It.IsAny<CancellationToken>()))
            .Callback<Guid, string, Stream, string?, CancellationToken>((_, _, stream, _, _) =>
            {
                streamPositionDuringUpload = stream.Position;
            })
            .ReturnsAsync(storagePath);

        _uploadRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Upload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Upload upload, CancellationToken _) =>
            {
                upload.Id = uploadId;
                return upload;
            });

        _itemRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<KnowledgeItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeItem item, CancellationToken _) =>
            {
                item.Id = itemId;
                return item;
            });

        // Act
        var result = await _sut.CreateNoteAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(itemId);
        result.Title.Should().Be("Test Note");
        result.Content.Should().Be("# Test Content");
        result.Tags.Should().Be("[\"test\"]");
        result.ItemType.Should().Be(KnowledgeItemType.Note);
        result.SourceUploadId.Should().Be(uploadId);

        _storageServiceMock.Verify(s => s.UploadAsync(userId, "Test Note.md", It.IsAny<Stream>(), "text/markdown", It.IsAny<CancellationToken>()), Times.Once);
        _uploadRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Upload>(), It.IsAny<CancellationToken>()), Times.Once);
        _uploadRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Upload>(u => u.Metadata == "[\"test\"]"),
            It.IsAny<CancellationToken>()), Times.Once);
        _itemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<KnowledgeItem>(), It.IsAny<CancellationToken>()), Times.Once);
        streamPositionDuringUpload.Should().Be(0);
    }

    [Test]
    public void CreateNoteAsync_NoCurrentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new CreateNoteRequest { FolderId = Guid.NewGuid(), Title = "Test" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns((Guid?)null);

        // Act
        var act = () => _sut.CreateNoteAsync(request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not authenticated");

        _folderRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void CreateNoteAsync_FolderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var request = new CreateNoteRequest { FolderId = folderId, Title = "Test" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Folder?)null);

        // Act
        var act = () => _sut.CreateNoteAsync(request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Folder not found");

        _kbRepositoryMock.Verify(r => r.GetOrCreateForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void CreateNoteAsync_FolderOwnedByDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherKbId = Guid.NewGuid();
        var userKbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var request = new CreateNoteRequest { FolderId = folderId, Title = "Test" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Folder { Id = folderId, KnowledgeBaseId = otherKbId, Name = "Other Folder" });

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = userKbId, UserId = userId });

        // Act
        var act = () => _sut.CreateNoteAsync(request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Folder does not belong to user");

        _storageServiceMock.Verify(s => s.UploadAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region CreateFromUploadAsync Tests

    [Test]
    public async Task CreateFromUploadAsync_ValidRequest_CreatesFileItemFromUpload()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var request = new CreateItemFromUploadRequest
        {
            FolderId = folderId,
            UploadId = uploadId,
            Title = "Custom Title",
            Tags = "[\"document\"]"
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Test Folder" });

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Upload { Id = uploadId, UserId = userId, FileName = "document.pdf", StoragePath = "uploads/document.pdf", Metadata = "[\"from-upload\"]" });

        _uploadRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Upload>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _itemRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<KnowledgeItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeItem item, CancellationToken _) =>
            {
                item.Id = itemId;
                return item;
            });

        // Act
        var result = await _sut.CreateFromUploadAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(itemId);
        result.Title.Should().Be("Custom Title");
        result.ItemType.Should().Be(KnowledgeItemType.File);
        result.Tags.Should().Be("[\"document\"]");
        result.SourceUploadId.Should().Be(uploadId);

        _itemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<KnowledgeItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _uploadRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<Upload>(u => u.Id == uploadId && u.Metadata == "[\"document\"]"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateFromUploadAsync_NoTitle_UsesFileNameAsTitle()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var request = new CreateItemFromUploadRequest
        {
            FolderId = folderId,
            UploadId = uploadId,
            Title = null // No title provided
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Test Folder" });

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Upload { Id = uploadId, UserId = userId, FileName = "document.pdf", StoragePath = "uploads/document.pdf", Metadata = "[\"from-upload\"]" });

        _itemRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<KnowledgeItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeItem item, CancellationToken _) =>
            {
                item.Id = itemId;
                return item;
            });

        // Act
        var result = await _sut.CreateFromUploadAsync(request, CancellationToken.None);

        // Assert
        result.Title.Should().Be("document.pdf");
        result.Tags.Should().Be("[\"from-upload\"]");
        _uploadRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Upload>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void CreateFromUploadAsync_UploadNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();

        var request = new CreateItemFromUploadRequest
        {
            FolderId = folderId,
            UploadId = uploadId
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Test Folder" });

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Upload?)null);

        // Act
        var act = () => _sut.CreateFromUploadAsync(request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Upload not found");

        _itemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<KnowledgeItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void CreateFromUploadAsync_UploadOwnedByDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();

        var request = new CreateItemFromUploadRequest
        {
            FolderId = folderId,
            UploadId = uploadId
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Test Folder" });

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Upload { Id = uploadId, UserId = otherUserId, FileName = "other.pdf", StoragePath = "uploads/other.pdf" });

        // Act
        var act = () => _sut.CreateFromUploadAsync(request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Upload does not belong to user");

        _itemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<KnowledgeItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Test]
    public async Task UpdateAsync_ValidRequest_UpdatesItem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var request = new UpdateItemRequest
        {
            Title = "Updated Title",
            Content = "Updated Content",
            Tags = "[\"updated\"]"
        };

        var kb = new KnowledgeBase { Id = kbId, UserId = userId };
        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, KnowledgeBase = kb, Name = "Test Folder" };
        var item = new KnowledgeItem { Id = itemId, FolderId = folderId, Title = "Old Title", Folder = folder };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _itemRepositoryMock
            .Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Act
        var result = await _sut.UpdateAsync(itemId, request, CancellationToken.None);

        // Assert
        result.Title.Should().Be("Updated Title");
        result.Content.Should().Be("Updated Content");
        result.Tags.Should().Be("[\"updated\"]");

        _itemRepositoryMock.Verify(r => r.UpdateAsync(It.Is<KnowledgeItem>(i => i.Title == "Updated Title"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_NoteContentOrTitleChanged_UpdatesStorageAndUploadRecord()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();
        var oldStoragePath = $"{userId}/old-note.md";
        var newStoragePath = $"{userId}/new-note.md";

        var request = new UpdateItemRequest
        {
            Title = "Updated Title",
            Content = "Updated Content",
            Tags = "[\"updated\"]"
        };

        var kb = new KnowledgeBase { Id = kbId, UserId = userId };
        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, KnowledgeBase = kb, Name = "Test Folder" };
        var item = new KnowledgeItem
        {
            Id = itemId,
            FolderId = folderId,
            Title = "Old Title",
            Content = "Old Content",
            ItemType = KnowledgeItemType.Note,
            SourceUploadId = uploadId,
            Folder = folder
        };
        var upload = new Upload
        {
            Id = uploadId,
            UserId = userId,
            FileName = "Old Title.md",
            StoragePath = oldStoragePath,
            ContentType = "text/markdown"
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _itemRepositoryMock
            .Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(upload);

        _uploadRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Upload>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _storageServiceMock
            .Setup(s => s.UploadAsync(userId, "Updated Title.md", It.IsAny<Stream>(), "text/markdown", It.IsAny<CancellationToken>()))
            .ReturnsAsync(newStoragePath);

        _storageServiceMock
            .Setup(s => s.DeleteAsync(oldStoragePath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateAsync(itemId, request, CancellationToken.None);

        // Assert
        result.Title.Should().Be("Updated Title");
        result.Content.Should().Be("Updated Content");
        result.Tags.Should().Be("[\"updated\"]");

        _storageServiceMock.Verify(s => s.UploadAsync(userId, "Updated Title.md", It.IsAny<Stream>(), "text/markdown", It.IsAny<CancellationToken>()), Times.Once);
        _storageServiceMock.Verify(s => s.DeleteAsync(oldStoragePath, It.IsAny<CancellationToken>()), Times.Once);
        _uploadRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<Upload>(u => u.Id == uploadId
                && u.FileName == "Updated Title.md"
                && u.StoragePath == newStoragePath
                && u.Metadata == "[\"updated\"]"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_FileTagsChanged_UpdatesUploadMetadataOnly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();

        var request = new UpdateItemRequest
        {
            Tags = "[\"file-updated\"]"
        };

        var kb = new KnowledgeBase { Id = kbId, UserId = userId };
        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, KnowledgeBase = kb, Name = "Test Folder" };
        var item = new KnowledgeItem
        {
            Id = itemId,
            FolderId = folderId,
            Title = "File Item",
            ItemType = KnowledgeItemType.File,
            SourceUploadId = uploadId,
            Folder = folder
        };
        var upload = new Upload
        {
            Id = uploadId,
            UserId = userId,
            FileName = "file.pdf",
            StoragePath = "uploads/file.pdf"
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _itemRepositoryMock
            .Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        _uploadRepositoryMock
            .Setup(r => r.GetByIdAsync(uploadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(upload);

        _uploadRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Upload>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateAsync(itemId, request, CancellationToken.None);

        // Assert
        result.Tags.Should().Be("[\"file-updated\"]");
        _uploadRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<Upload>(u => u.Id == uploadId && u.Metadata == "[\"file-updated\"]"),
            It.IsAny<CancellationToken>()), Times.Once);
        _storageServiceMock.Verify(s => s.UploadAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _storageServiceMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_MoveToNewFolder_UpdatesFolderId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var newFolderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var request = new UpdateItemRequest { FolderId = newFolderId };

        var kb = new KnowledgeBase { Id = kbId, UserId = userId };
        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, KnowledgeBase = kb, Name = "Test Folder" };
        var newFolder = new Folder { Id = newFolderId, KnowledgeBaseId = kbId, KnowledgeBase = kb, Name = "New Folder" };
        var item = new KnowledgeItem { Id = itemId, FolderId = folderId, Title = "Test", Folder = folder };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _itemRepositoryMock
            .Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(newFolderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newFolder);

        // Act
        var result = await _sut.UpdateAsync(itemId, request, CancellationToken.None);

        // Assert
        result.FolderId.Should().Be(newFolderId);
        item.FolderId.Should().Be(newFolderId);
    }

    [Test]
    public void UpdateAsync_ItemNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new UpdateItemRequest { Title = "Updated" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _itemRepositoryMock
            .Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeItem?)null);

        // Act
        var act = () => _sut.UpdateAsync(itemId, request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Item not found");

        _itemRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<KnowledgeItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void UpdateAsync_ItemOwnedByDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var request = new UpdateItemRequest { Title = "Updated" };

        var otherKb = new KnowledgeBase { Id = kbId, UserId = otherUserId };
        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, KnowledgeBase = otherKb, Name = "Other Folder" };
        var item = new KnowledgeItem { Id = itemId, FolderId = folderId, Title = "Test", Folder = folder };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _itemRepositoryMock
            .Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Act
        var act = () => _sut.UpdateAsync(itemId, request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Item does not belong to user");

        _itemRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<KnowledgeItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void UpdateAsync_TargetFolderOwnedByDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var otherKbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var newFolderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var request = new UpdateItemRequest { FolderId = newFolderId };

        var userKb = new KnowledgeBase { Id = kbId, UserId = userId };
        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, KnowledgeBase = userKb, Name = "User Folder" };
        var otherKb = new KnowledgeBase { Id = otherKbId, UserId = otherUserId };
        var newFolder = new Folder { Id = newFolderId, KnowledgeBaseId = otherKbId, KnowledgeBase = otherKb, Name = "Other Folder" };
        var item = new KnowledgeItem { Id = itemId, FolderId = folderId, Title = "Test", Folder = folder };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _itemRepositoryMock
            .Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(newFolderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newFolder);

        // Act
        var act = () => _sut.UpdateAsync(itemId, request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Target folder does not belong to user");

        _itemRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<KnowledgeItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Test]
    public async Task DeleteAsync_ValidRequest_DeletesItemButNotUpload()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();

        var kb = new KnowledgeBase { Id = kbId, UserId = userId };
        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, KnowledgeBase = kb, Name = "Test Folder" };
        var item = new KnowledgeItem
        {
            Id = itemId,
            FolderId = folderId,
            Title = "Test Note",
            ItemType = KnowledgeItemType.Note,
            SourceUploadId = uploadId,
            Folder = folder
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _itemRepositoryMock
            .Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Act
        await _sut.DeleteAsync(itemId, CancellationToken.None);

        // Assert
        _itemRepositoryMock.Verify(r => r.DeleteAsync(itemId, It.IsAny<CancellationToken>()), Times.Once);
        // Upload should NOT be deleted (unlink only per plan)
        _uploadRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteAsync_ItemNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _itemRepositoryMock
            .Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeItem?)null);

        // Act
        var act = () => _sut.DeleteAsync(itemId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Item not found");

        _itemRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteAsync_ItemOwnedByDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var otherKb = new KnowledgeBase { Id = kbId, UserId = otherUserId };
        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, KnowledgeBase = otherKb, Name = "Other Folder" };
        var item = new KnowledgeItem { Id = itemId, FolderId = folderId, Title = "Test", Folder = folder };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _itemRepositoryMock
            .Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Act
        var act = () => _sut.DeleteAsync(itemId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Item does not belong to user");

        _itemRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetByIdAsync Tests

    [Test]
    public async Task GetByIdAsync_ValidRequest_ReturnsItem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var kb = new KnowledgeBase { Id = kbId, UserId = userId };
        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, KnowledgeBase = kb, Name = "Test Folder" };
        var item = new KnowledgeItem
        {
            Id = itemId,
            FolderId = folderId,
            Title = "Test Item",
            ItemType = KnowledgeItemType.Note,
            Folder = folder
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _itemRepositoryMock
            .Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Act
        var result = await _sut.GetByIdAsync(itemId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(itemId);
        result.Title.Should().Be("Test Item");
    }

    [Test]
    public async Task GetByIdAsync_ItemNotFound_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _itemRepositoryMock
            .Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeItem?)null);

        // Act
        var result = await _sut.GetByIdAsync(itemId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void GetByIdAsync_ItemOwnedByDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var otherKb = new KnowledgeBase { Id = kbId, UserId = otherUserId };
        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, KnowledgeBase = otherKb, Name = "Other Folder" };
        var item = new KnowledgeItem { Id = itemId, FolderId = folderId, Title = "Test", Folder = folder };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _itemRepositoryMock
            .Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Act
        var act = () => _sut.GetByIdAsync(itemId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Item does not belong to user");
    }

    #endregion

    #region SearchAsync Tests

    [Test]
    public async Task SearchAsync_ValidRequest_ReturnsPagedResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var request = new ItemSearchRequest
        {
            Search = "test",
            Tags = "[\"tag1\"]",
            Type = KnowledgeItemType.Note,
            Page = 1,
            PageSize = 10
        };

        var items = new List<KnowledgeItem>
        {
            new() { Id = itemId, Title = "Test Item", ItemType = KnowledgeItemType.Note }
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _itemRepositoryMock
            .Setup(r => r.SearchAsync(kbId, null, null, "test", "[\"tag1\"]", KnowledgeItemType.Note, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<KnowledgeItem>
            {
                Items = items,
                Page = 1,
                PageSize = 10,
                TotalCount = 1
            });

        // Act
        var result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(itemId);
    }

    [Test]
    public async Task SearchAsync_IncludeSubFolders_ResolvesDescendantFolderIds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var parentFolderId = Guid.NewGuid();
        var childFolderId = Guid.NewGuid();
        var grandChildFolderId = Guid.NewGuid();

        var request = new ItemSearchRequest
        {
            FolderId = parentFolderId,
            IncludeSubFolders = true,
            Page = 1,
            PageSize = 10
        };

        var folderTree = new List<Folder>
        {
            new() { Id = parentFolderId, KnowledgeBaseId = kbId, Name = "Parent" },
            new() { Id = childFolderId, KnowledgeBaseId = kbId, ParentFolderId = parentFolderId, Name = "Child" },
            new() { Id = grandChildFolderId, KnowledgeBaseId = kbId, ParentFolderId = childFolderId, Name = "GrandChild" }
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetFolderTreeAsync(kbId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folderTree);

        _itemRepositoryMock
            .Setup(r => r.SearchAsync(
                kbId,
                parentFolderId,
                It.Is<IReadOnlyCollection<Guid>?>(ids =>
                    ids != null
                    && ids.Contains(parentFolderId)
                    && ids.Contains(childFolderId)
                    && ids.Contains(grandChildFolderId)
                    && ids.Count == 3),
                null,
                null,
                null,
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<KnowledgeItem>
            {
                Items = [],
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            });

        // Act
        var result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(0);
        _folderRepositoryMock.Verify(r => r.GetFolderTreeAsync(kbId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void SearchAsync_NoCurrentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new ItemSearchRequest { Page = 1, PageSize = 10 };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns((Guid?)null);

        // Act
        var act = () => _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not authenticated");

        _kbRepositoryMock.Verify(r => r.GetOrCreateForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _itemRepositoryMock.Verify(r => r.SearchAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<IReadOnlyCollection<Guid>?>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<KnowledgeItemType?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
