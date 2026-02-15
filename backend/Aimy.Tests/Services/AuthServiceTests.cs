using Aimy.Core.Application.Interfaces;
using Aimy.Core.Application.Interfaces.Auth;
using Aimy.Core.Application.Services;
using Aimy.Core.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Aimy.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private Mock<IPasswordHasher> _passwordHasherMock = null!;
    private Mock<ITokenProvider> _tokenProviderMock = null!;
    private AuthService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenProviderMock = new Mock<ITokenProvider>();
        _sut = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenProviderMock.Object);
    }

    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            PasswordHash = "hashed",
            Role = "Admin"
        };
        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _passwordHasherMock
            .Setup(h => h.Verify("password", "hashed"))
            .Returns(true);
        _tokenProviderMock
            .Setup(t => t.GenerateToken(user))
            .Returns("jwt-token");

        // Act
        var result = await _sut.LoginAsync("testuser", "password");

        // Assert
        result.Should().Be("jwt-token");
    }

    [Test]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            PasswordHash = "hashed"
        };
        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _passwordHasherMock
            .Setup(h => h.Verify("wrong", "hashed"))
            .Returns(false);

        // Act
        var result = await _sut.LoginAsync("testuser", "wrong");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task LoginAsync_UnknownUser_ReturnsNull()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync("unknown"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.LoginAsync("unknown", "password");

        // Assert
        result.Should().BeNull();
    }
}
