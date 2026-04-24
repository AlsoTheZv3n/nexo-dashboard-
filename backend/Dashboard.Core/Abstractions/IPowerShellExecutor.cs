namespace Dashboard.Core.Abstractions;

public sealed record PowerShellResult(string Stdout, string Stderr, int ExitCode, bool TimedOut);

public interface IPowerShellExecutor
{
    /// <summary>
    /// Runs the script at <paramref name="scriptPath"/> with the given parameters in an isolated runspace.
    /// Returns the captured stdout, stderr, and a UNIX-style exit code (0 = success, 1 = errors, 124 = timeout).
    /// </summary>
    Task<PowerShellResult> ExecuteAsync(
        string scriptPath,
        IReadOnlyDictionary<string, object?> parameters,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
