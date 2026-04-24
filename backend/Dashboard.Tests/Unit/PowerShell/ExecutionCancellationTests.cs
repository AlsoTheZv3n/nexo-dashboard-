using Dashboard.PowerShell;

namespace Dashboard.Tests.Unit.PowerShell;

public class ExecutionCancellationTests
{
    [Fact]
    public void Cancel_OnUnknownExecution_ReturnsFalse()
    {
        var sut = new ExecutionCancellation();
        sut.Cancel(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Register_ThenCancel_SignalsTheLinkedToken()
    {
        var sut = new ExecutionCancellation();
        var id = Guid.NewGuid();

        using var cts = sut.Register(id, CancellationToken.None);
        var signalled = sut.Cancel(id);

        signalled.Should().BeTrue();
        cts.Token.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public void Remove_DropsTheEntry()
    {
        var sut = new ExecutionCancellation();
        var id = Guid.NewGuid();

        using (sut.Register(id, CancellationToken.None))
        {
            sut.Remove(id);
        }

        sut.Cancel(id).Should().BeFalse();
    }

    [Fact]
    public void LinkedToken_InheritsParentCancellation()
    {
        var sut = new ExecutionCancellation();
        using var outer = new CancellationTokenSource();

        using var cts = sut.Register(Guid.NewGuid(), outer.Token);
        outer.Cancel();

        cts.Token.IsCancellationRequested.Should().BeTrue();
    }
}
