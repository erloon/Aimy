using Aimy.Core.Application.Interfaces;
using Aimy.Core.Application.Services;
using Aimy.Core.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Aimy.Tests.Services;

[TestFixture]
public class KnowledgeBaseServiceTests
{
    private Mock<IKnowledgeBaseRepository> _kbRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private KnowledgeBaseService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _kbRepositoryMock = new Mock<IKnowledgeBaseRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _sut = new KnowledgeBaseService(
            _kbRepositoryMock.Object,
            _currentUserServiceMock.Object);
    }

    #region GetOrCreateAsync Tests

    [Test]
    public async Task GetOrCreateAsync_NoExistingKb_CreatesNewKbForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();

        var kb = new KnowledgeBase { Id = kbId, UserId = userId };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(kb);

        // Act
        var result = await _sut.GetOrCreateAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(kbId);
        result.UserId.Should().Be(userId);

        _kbRepositoryMock.Verify(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetOrCreateAsync_ExistingKb_ReturnsExistingKb()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var kbId = Guid.NewGuid();

        var existingKb = new KnowledgeBase { Id = kbId, UserId = userId };

        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        _kbRepositoryMock
            .Setup(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingKb);

        // Act
        var result = await _sut.GetOrCreateAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(kbId);
        result.UserId.Should().Be(userId);

        // Verify it returns the existing KB (repository handles the get-or-create logic)
        _kbRepositoryMock.Verify(r => r.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void GetOrCreateAsync_NoCurrentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _currentUserServiceMock
            .Setup(s => s.GetCurrentUserId())
            .Returns((Guid?)null);

        // Act
        var act = () => _sut.GetOrCreateAsync(CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not authenticated");

        _kbRepositoryMock.Verify(r => r.GetOrCreateForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
