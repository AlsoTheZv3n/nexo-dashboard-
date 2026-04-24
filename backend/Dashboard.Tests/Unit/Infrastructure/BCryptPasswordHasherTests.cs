using Dashboard.Infrastructure.Auth;

namespace Dashboard.Tests.Unit.Infrastructure;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_ProducesBcryptFormat()
    {
        var hash = _sut.Hash("secret");
        hash.Should().StartWith("$2");
        hash.Length.Should().BeGreaterThan(50);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _sut.Hash("secret");
        _sut.Verify("secret", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _sut.Hash("secret");
        _sut.Verify("wrong", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_MalformedHash_ReturnsFalseNotThrows()
    {
        _sut.Verify("x", "not-a-bcrypt-hash").Should().BeFalse();
    }
}
