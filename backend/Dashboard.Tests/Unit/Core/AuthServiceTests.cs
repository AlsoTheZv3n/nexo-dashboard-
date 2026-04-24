using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Dashboard.Core.Services;
using Moq;

namespace Dashboard.Tests.Unit.Core;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IClock> _clock = new();

    private AuthService Sut() => new(_users.Object, _hasher.Object, _tokens.Object, _clock.Object);

    [Fact]
    public async Task Login_WithUnknownUser_ReturnsUnauthorized()
    {
        _users.Setup(u => u.GetByUsernameAsync("ghost", It.IsAny<CancellationToken>()))
              .ReturnsAsync((User?)null);

        var result = await Sut().LoginAsync("ghost", "pw");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var user = new User("alice", "hash", UserRole.Admin);
        _users.Setup(u => u.GetByUsernameAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("wrong", "hash")).Returns(false);

        var result = await Sut().LoginAsync("alice", "wrong");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsUnauthorized()
    {
        var user = new User("alice", "hash", UserRole.Admin);
        user.Deactivate();
        _users.Setup(u => u.GetByUsernameAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await Sut().LoginAsync("alice", "pw");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Login_WithEmptyInput_ReturnsValidation()
    {
        var result = await Sut().LoginAsync("", "pw");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task Login_HappyPath_ReturnsTokensAndRecordsLogin()
    {
        var user = new User("alice", "hash", UserRole.Admin);
        var loginTime = new DateTimeOffset(2026, 4, 24, 10, 0, 0, TimeSpan.Zero);
        var pair = new TokenPair("access", "refresh", loginTime.AddMinutes(15));

        _users.Setup(u => u.GetByUsernameAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("correct", "hash")).Returns(true);
        _clock.Setup(c => c.UtcNow).Returns(loginTime);
        _tokens.Setup(t => t.CreateTokens(user)).Returns(pair);

        var result = await Sut().LoginAsync("alice", "correct");

        result.IsSuccess.Should().BeTrue();
        result.Value!.User.Should().BeSameAs(user);
        result.Value!.Tokens.AccessToken.Should().Be("access");
        user.LastLoginAt.Should().Be(loginTime);
        _users.Verify(u => u.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        _tokens.Setup(t => t.ValidateRefreshToken("bad")).Returns((Guid?)null);

        var result = await Sut().RefreshAsync("bad");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        var user = new User("alice", "hash", UserRole.Admin);
        _tokens.Setup(t => t.ValidateRefreshToken("good")).Returns(user.Id);
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokens.Setup(t => t.CreateTokens(user)).Returns(new TokenPair("a", "r", DateTimeOffset.UtcNow));

        var result = await Sut().RefreshAsync("good");

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("a");
    }
}
