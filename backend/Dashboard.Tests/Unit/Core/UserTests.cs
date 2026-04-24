using Dashboard.Core.Entities;

namespace Dashboard.Tests.Unit.Core;

public class UserTests
{
    [Fact]
    public void NewUser_IsActive_AndRecordsCreationTime()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var user = new User("alice", "hash", UserRole.Operator);

        user.Id.Should().NotBe(Guid.Empty);
        user.Username.Should().Be("alice");
        user.Role.Should().Be(UserRole.Operator);
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeAfter(before);
        user.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public void RecordLogin_SetsLastLoginAt()
    {
        var user = new User("bob", "hash", UserRole.Viewer);
        var when = new DateTimeOffset(2026, 4, 24, 10, 0, 0, TimeSpan.Zero);

        user.RecordLogin(when);

        user.LastLoginAt.Should().Be(when);
    }

    [Fact]
    public void Deactivate_FlipsIsActive()
    {
        var user = new User("carol", "hash", UserRole.Admin);
        user.Deactivate();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ChangePassword_RejectsEmpty()
    {
        var user = new User("dan", "old", UserRole.Admin);
        Action act = () => user.ChangePassword("  ");
        act.Should().Throw<ArgumentException>().WithParameterName("newHash");
    }
}
