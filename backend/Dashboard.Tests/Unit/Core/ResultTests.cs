using Dashboard.Core.Common;

namespace Dashboard.Tests.Unit.Core;

public class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var r = Result.Success();
        r.IsSuccess.Should().BeTrue();
        r.IsFailure.Should().BeFalse();
        r.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_CarriesError()
    {
        var r = Result.Failure(Error.Validation("bad"));
        r.IsSuccess.Should().BeFalse();
        r.Error.Code.Should().Be("Validation");
    }

    [Fact]
    public void GenericResult_ImplicitConversion_FromValue()
    {
        Result<int> r = 42;
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(42);
    }

    [Fact]
    public void GenericResult_ImplicitConversion_FromError()
    {
        Result<int> r = Error.NotFound("Thing");
        r.IsFailure.Should().BeTrue();
        r.Error.Code.Should().Be("Thing.NotFound");
    }
}
