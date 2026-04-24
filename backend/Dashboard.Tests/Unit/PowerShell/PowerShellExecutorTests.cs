using Dashboard.PowerShell;

namespace Dashboard.Tests.Unit.PowerShell;

public class PowerShellExecutorTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "ps-exec-" + Guid.NewGuid().ToString("N"));
    private readonly PowerShellExecutor _sut = new();

    public PowerShellExecutorTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
    }

    private string WriteScript(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task Execute_SimpleScript_CapturesStdoutAndReturnsExit0()
    {
        var path = WriteScript("hello.ps1", "Write-Output 'hello'");
        var result = await _sut.ExecuteAsync(path, new Dictionary<string, object?>(), TimeSpan.FromSeconds(10));

        result.ExitCode.Should().Be(0);
        result.TimedOut.Should().BeFalse();
        result.Stdout.Trim().Should().Be("hello");
    }

    [Fact]
    public async Task Execute_WithParameter_PassesItIn()
    {
        var path = WriteScript(
            "echo.ps1",
            "param([string]$Msg) Write-Output \"got:$Msg\"");

        var result = await _sut.ExecuteAsync(
            path,
            new Dictionary<string, object?> { ["Msg"] = "ping" },
            TimeSpan.FromSeconds(10));

        result.ExitCode.Should().Be(0);
        result.Stdout.Trim().Should().Be("got:ping");
    }

    [Fact]
    public async Task Execute_WritesToErrorStream_ReportsExit1()
    {
        var path = WriteScript("boom.ps1", "Write-Error 'something broke'");
        var result = await _sut.ExecuteAsync(path, new Dictionary<string, object?>(), TimeSpan.FromSeconds(10));

        result.ExitCode.Should().Be(1);
        result.Stderr.Should().Contain("something broke");
    }

    [Fact]
    public async Task Execute_LongRunning_TimesOut()
    {
        var path = WriteScript("sleepy.ps1", "Start-Sleep -Seconds 30");
        var result = await _sut.ExecuteAsync(path, new Dictionary<string, object?>(), TimeSpan.FromMilliseconds(300));

        result.TimedOut.Should().BeTrue();
        result.ExitCode.Should().Be(124);
    }

    [Fact]
    public async Task Execute_MissingScript_Throws()
    {
        var path = Path.Combine(_tempDir, "ghost.ps1");
        var act = () => _sut.ExecuteAsync(path, new Dictionary<string, object?>(), TimeSpan.FromSeconds(1));
        await act.Should().ThrowAsync<FileNotFoundException>();
    }
}
