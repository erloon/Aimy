using Aimy.Core.Application.DTOs.KnowledgeBase;
using Aimy.Core.Application.Interfaces.Auth;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Application.Services;
using Aimy.Core.Domain.Entities;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aimy.Tests.Services;

[TestFixture]
public class FolderServiceTests
{
    private Mock<IKnowledgeBaseRepository> _kbRepositoryMock = null!;
    private Mock<IFolderRepository> _folderRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private FolderService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _kbRepositoryMock = new Mock<IKnowledgeBaseRepository>();
        _folderRepositoryMock = new Mock<IFolderRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _sut = new FolderService(
            _kbRepositoryMock.Object,
            _folderRepositoryMock.Object,
            _currentUserServiceMock.Object,
            NullLogger<FolderService>.Instance);
    }

    #region CreateAsync Tests

    [Test]
    public async Task CreateAsync_ValidRequest_CreatesFolder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var request = new CreateFolderRequest { Name = "Test Folder", ParentFolderId = null };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Folder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Folder folder, CancellationToken _) =>
            {
                folder.Id = folderId;
                return folder;
            });

        // Act
        var result = await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(folderId);
        result.Name.Should().Be("Test Folder");
        result.KnowledgeBaseId.Should().Be(kbId);
        result.ParentFolderId.Should().BeNull();

        _folderRepositoryMock.Verify(r => r.AddAsync(It.Is<Folder>(f => f.Name == "Test Folder"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void CreateAsync_NoCurrentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new CreateFolderRequest { Name = "Test Folder" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns((Guid?)null);

        // Act
        var act = () => _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not authenticated");

        _kbRepositoryMock.Verify(r => r.GetOrCreateForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _folderRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Folder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void CreateAsync_InvalidParentFolder_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var parentFolderId = Guid.NewGuid();
        var request = new CreateFolderRequest { Name = "Test Folder", ParentFolderId = parentFolderId };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(parentFolderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Folder?)null);

        // Act
        var act = () => _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Parent folder not found");

        _folderRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Folder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void CreateAsync_ParentFolderOwnedByDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var otherKbId = Guid.NewGuid();
        var parentFolderId = Guid.NewGuid();
        var request = new CreateFolderRequest { Name = "Test Folder", ParentFolderId = parentFolderId };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(parentFolderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Folder { Id = parentFolderId, KnowledgeBaseId = otherKbId, Name = "Other Folder" });

        // Act
        var act = () => _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Parent folder does not belong to user");

        _folderRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Folder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Test]
    public async Task UpdateAsync_ValidRequest_UpdatesFolderName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var request = new UpdateFolderRequest { Name = "Updated Name" };

        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Old Name" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        // Act
        var result = await _sut.UpdateAsync(folderId, request, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Updated Name");
        folder.Name.Should().Be("Updated Name");

        _folderRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Folder>(f => f.Name == "Updated Name"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UpdateAsync_FolderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var request = new UpdateFolderRequest { Name = "Updated Name" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Folder?)null);

        // Act
        var act = () => _sut.UpdateAsync(folderId, request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Folder not found");

        _folderRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Folder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void UpdateAsync_FolderOwnedByDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var otherKbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var request = new UpdateFolderRequest { Name = "Updated Name" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Folder { Id = folderId, KnowledgeBaseId = otherKbId, Name = "Other Folder" });

        // Act
        var act = () => _sut.UpdateAsync(folderId, request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Folder does not belong to user");

        _folderRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Folder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region MoveAsync Tests

    [Test]
    public async Task MoveAsync_ValidRequest_MovesFolder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var newParentId = Guid.NewGuid();
        var request = new MoveFolderRequest { NewParentFolderId = newParentId };

        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Test Folder" };
        var newParent = new Folder { Id = newParentId, KnowledgeBaseId = kbId, Name = "Parent Folder" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(newParentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newParent);

        // Act
        var result = await _sut.MoveAsync(folderId, request, CancellationToken.None);

        // Assert
        result.ParentFolderId.Should().Be(newParentId);
        folder.ParentFolderId.Should().Be(newParentId);

        _folderRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Folder>(f => f.ParentFolderId == newParentId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void MoveAsync_MovingToSelf_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var request = new MoveFolderRequest { NewParentFolderId = folderId };

        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Test Folder" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        // Act
        var act = () => _sut.MoveAsync(folderId, request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot move folder to itself");

        _folderRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Folder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void MoveAsync_MovingToDescendant_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var childFolderId = Guid.NewGuid();
        var request = new MoveFolderRequest { NewParentFolderId = childFolderId };

        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Parent Folder" };
        var childFolder = new Folder { Id = childFolderId, KnowledgeBaseId = kbId, ParentFolderId = folderId, Name = "Child Folder" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        // When checking if childFolderId is a descendant of folderId
        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(childFolderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(childFolder);

        // Act
        var act = () => _sut.MoveAsync(folderId, request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot move folder to its own descendant");

        _folderRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Folder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void MoveAsync_TargetFolderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var newParentId = Guid.NewGuid();
        var request = new MoveFolderRequest { NewParentFolderId = newParentId };

        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Test Folder" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(newParentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Folder?)null);

        // Act
        var act = () => _sut.MoveAsync(folderId, request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Target folder not found");

        _folderRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Folder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void MoveAsync_TargetFolderOwnedByDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var otherKbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var newParentId = Guid.NewGuid();
        var request = new MoveFolderRequest { NewParentFolderId = newParentId };

        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Test Folder" };
        var newParent = new Folder { Id = newParentId, KnowledgeBaseId = otherKbId, Name = "Other Folder" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(newParentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newParent);

        // Act
        var act = () => _sut.MoveAsync(folderId, request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Target folder does not belong to user");

        _folderRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Folder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Test]
    public async Task DeleteAsync_ValidRequest_DeletesFolder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Test Folder" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        _folderRepositoryMock
            .Setup(r => r.HasChildrenAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _folderRepositoryMock
            .Setup(r => r.HasItemsAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _sut.DeleteAsync(folderId, false, CancellationToken.None);

        // Assert
        _folderRepositoryMock.Verify(r => r.DeleteAsync(folderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_ForceTrue_DeletesFolderWithContents()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Test Folder" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        // Act
        await _sut.DeleteAsync(folderId, true, CancellationToken.None);

        // Assert
        _folderRepositoryMock.Verify(r => r.DeleteWithContentsAsync(folderId, It.IsAny<CancellationToken>()), Times.Once);
        _folderRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _folderRepositoryMock.Verify(r => r.HasChildrenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _folderRepositoryMock.Verify(r => r.HasItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteAsync_FolderWithChildren_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Test Folder" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        _folderRepositoryMock
            .Setup(r => r.HasChildrenAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _sut.DeleteAsync(folderId, false, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete folder with subfolders");

        _folderRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteAsync_FolderWithItems_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Test Folder" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        _folderRepositoryMock
            .Setup(r => r.HasChildrenAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _folderRepositoryMock
            .Setup(r => r.HasItemsAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _sut.DeleteAsync(folderId, false, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete folder with items");

        _folderRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteAsync_FolderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Folder?)null);

        // Act
        var act = () => _sut.DeleteAsync(folderId, false, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Folder not found");

        _folderRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteAsync_ForceTrue_NoCurrentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var folderId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns((Guid?)null);

        // Act
        var act = () => _sut.DeleteAsync(folderId, true, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not authenticated");

        _kbRepositoryMock.Verify(r => r.GetOrCreateForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _folderRepositoryMock.Verify(r => r.DeleteWithContentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteAsync_ForceTrue_FolderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Folder?)null);

        // Act
        var act = () => _sut.DeleteAsync(folderId, true, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Folder not found");

        _folderRepositoryMock.Verify(r => r.DeleteWithContentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteAsync_ForceTrue_FolderOwnedByDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var otherKbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Folder { Id = folderId, KnowledgeBaseId = otherKbId, Name = "Other Folder" });

        // Act
        var act = () => _sut.DeleteAsync(folderId, true, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Folder does not belong to user");

        _folderRepositoryMock.Verify(r => r.DeleteWithContentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetContentSummaryAsync Tests

    [Test]
    public async Task GetContentSummaryAsync_ValidRequest_ReturnsSummary()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Test Folder" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        _folderRepositoryMock
            .Setup(r => r.GetContentSummaryAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((3, 2));

        // Act
        var result = await _sut.GetContentSummaryAsync(folderId, CancellationToken.None);

        // Assert
        result.ItemCount.Should().Be(3);
        result.SubfolderCount.Should().Be(2);
        result.HasContent.Should().BeTrue();
    }

    [Test]
    public void GetContentSummaryAsync_NoCurrentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var folderId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns((Guid?)null);

        // Act
        var act = () => _sut.GetContentSummaryAsync(folderId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not authenticated");

        _kbRepositoryMock.Verify(r => r.GetOrCreateForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _folderRepositoryMock.Verify(r => r.GetContentSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void GetContentSummaryAsync_FolderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Folder?)null);

        // Act
        var act = () => _sut.GetContentSummaryAsync(folderId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Folder not found");

        _folderRepositoryMock.Verify(r => r.GetContentSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void GetContentSummaryAsync_FolderOwnedByDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var otherKbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Folder { Id = folderId, KnowledgeBaseId = otherKbId, Name = "Other Folder" });

        // Act
        var act = () => _sut.GetContentSummaryAsync(folderId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Folder does not belong to user");

        _folderRepositoryMock.Verify(r => r.GetContentSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetContentSummaryAsync_EmptyFolder_ReturnsSummaryWithHasContentFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Empty Folder" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        _folderRepositoryMock
            .Setup(r => r.GetContentSummaryAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0));

        // Act
        var result = await _sut.GetContentSummaryAsync(folderId, CancellationToken.None);

        // Assert
        result.ItemCount.Should().Be(0);
        result.SubfolderCount.Should().Be(0);
        result.HasContent.Should().BeFalse();
    }

    [Test]
    public async Task GetContentSummaryAsync_FolderWithItemsOnly_ReturnsSummaryWithHasContentTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        var folder = new Folder { Id = folderId, KnowledgeBaseId = kbId, Name = "Items Folder" };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        _folderRepositoryMock
            .Setup(r => r.GetContentSummaryAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((4, 0));

        // Act
        var result = await _sut.GetContentSummaryAsync(folderId, CancellationToken.None);

        // Assert
        result.ItemCount.Should().Be(4);
        result.SubfolderCount.Should().Be(0);
        result.HasContent.Should().BeTrue();
    }

    #endregion

    #region GetTreeAsync Tests

    [Test]
    public async Task GetTreeAsync_NoKbExists_ReturnsEmptyTree()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeBase?)null);

        // Act
        var result = await _sut.GetTreeAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RootFolders.Should().BeEmpty();

        _folderRepositoryMock.Verify(r => r.GetFolderTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetTreeAsync_FoldersExist_ReturnsTreeStructure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var parentFolderId = Guid.NewGuid();
        var childFolderId = Guid.NewGuid();

        var folders = new List<Folder>
        {
            new() { Id = parentFolderId, KnowledgeBaseId = kbId, Name = "Parent", ParentFolderId = null },
            new() { Id = childFolderId, KnowledgeBaseId = kbId, Name = "Child", ParentFolderId = parentFolderId }
        };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeBase { Id = kbId, UserId = userId });

        _folderRepositoryMock
            .Setup(r => r.GetFolderTreeAsync(kbId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folders);

        // Act
        var result = await _sut.GetTreeAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RootFolders.Should().HaveCount(1);
        result.RootFolders[0].Id.Should().Be(parentFolderId);
        result.RootFolders[0].Name.Should().Be("Parent");
        result.RootFolders[0].Children.Should().HaveCount(1);
        result.RootFolders[0].Children[0].Id.Should().Be(childFolderId);
        result.RootFolders[0].Children[0].Name.Should().Be("Child");
    }

    [Test]
    public void GetTreeAsync_NoCurrentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns((Guid?)null);

        // Act
        var act = () => _sut.GetTreeAsync(CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not authenticated");

        _kbRepositoryMock.Verify(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
