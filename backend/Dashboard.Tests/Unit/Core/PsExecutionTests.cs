using Dashboard.Core.Entities;

namespace Dashboard.Tests.Unit.Core;

public class PsExecutionTests
{
    [Fact]
    public void NewExecution_IsPending()
    {
        var e = new PsExecution(Guid.NewGuid(), Guid.NewGuid(), "{}");
        e.Status.Should().Be(ExecutionStatus.Pending);
        e.StartedAt.Should().BeNull();
        e.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void MarkCompleted_ExitCode0_IsCompleted()
    {
        var e = new PsExecution(Guid.NewGuid(), Guid.NewGuid(), "{}");
        e.MarkRunning();
        e.MarkCompleted("ok", null, 0);

        e.Status.Should().Be(ExecutionStatus.Completed);
        e.ExitCode.Should().Be(0);
        e.Stdout.Should().Be("ok");
        e.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkCompleted_NonZeroExitCode_IsFailed()
    {
        var e = new PsExecution(Guid.NewGuid(), Guid.NewGuid(), "{}");
        e.MarkRunning();
        e.MarkCompleted(null, "boom", 1);

        e.Status.Should().Be(ExecutionStatus.Failed);
        e.ExitCode.Should().Be(1);
        e.Stderr.Should().Be("boom");
    }

    [Fact]
    public void MarkCancelled_SetsStatusAndCompletedAt()
    {
        var e = new PsExecution(Guid.NewGuid(), Guid.NewGuid(), "{}");
        e.MarkRunning();
        e.MarkCancelled();

        e.Status.Should().Be(ExecutionStatus.Cancelled);
        e.CompletedAt.Should().NotBeNull();
    }
}
