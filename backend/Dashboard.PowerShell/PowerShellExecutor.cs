using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using Dashboard.Core.Abstractions;

namespace Dashboard.PowerShell;

public sealed class PowerShellExecutor : IPowerShellExecutor
{
    public async Task<PowerShellResult> ExecuteAsync(
        string scriptPath,
        IReadOnlyDictionary<string, object?> parameters,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException("PowerShell script not found.", scriptPath);

        var initial = InitialSessionState.CreateDefault();
        initial.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;

        using var runspace = RunspaceFactory.CreateRunspace(initial);
        runspace.Open();

        using var ps = System.Management.Automation.PowerShell.Create();
        ps.Runspace = runspace;

        // Reading the script body and invoking via AddScript sidesteps file-path policy
        // checks that the signed-script pipeline can raise on Windows even when
        // ExecutionPolicy=Bypass is configured at runspace level.
        var scriptBody = File.ReadAllText(scriptPath);
        ps.AddScript(scriptBody);
        foreach (var (name, value) in parameters)
        {
            ps.AddParameter(name, value);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        var output = new PSDataCollection<PSObject>();
        output.DataAdded += (_, e) =>
        {
            var item = output[e.Index];
            if (item is not null) stdout.AppendLine(item.ToString());
        };

        ps.Streams.Error.DataAdded += (_, e) =>
        {
            var err = ps.Streams.Error[e.Index];
            stderr.AppendLine(err.ToString());
        };

        var input = new PSDataCollection<PSObject>();
        input.Complete();

        var asyncResult = ps.BeginInvoke(input, output);
        var timedOut = false;

        try
        {
            using var reg = timeoutCts.Token.Register(() =>
            {
                try { ps.Stop(); } catch { /* runspace may be gone */ }
            });

            await Task.Factory.FromAsync(asyncResult, _ => ps.EndInvoke(asyncResult)).ConfigureAwait(false);
        }
        catch (PipelineStoppedException)
        {
            timedOut = cancellationToken.IsCancellationRequested is false;
        }

        var hadErrors = ps.HadErrors;
        var exitCode = timedOut ? 124 : hadErrors ? 1 : 0;
        return new PowerShellResult(stdout.ToString(), stderr.ToString(), exitCode, timedOut);
    }
}
