using System.Text.Json;
using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.PowerShell;

/// <summary>
/// Runs a queued PsExecution end-to-end: resolves the script, invokes it via
/// <see cref="IPowerShellExecutor"/>, and persists the outcome. Opens its own DI scope so it
/// can be fired from an ASP.NET request handler via Task.Run without leaking request-scoped services.
/// </summary>
public sealed class ExecutionRunner(
    IServiceScopeFactory scopes,
    IPowerShellExecutor executor,
    IOptions<PowerShellOptions> options,
    ILogger<ExecutionRunner> logger)
{
    private readonly PowerShellOptions _opts = options.Value;

    public async Task RunAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        using var scope = scopes.CreateScope();
        var executions = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
        var scripts = scope.ServiceProvider.GetRequiredService<IScriptRepository>();

        var execution = await executions.GetByIdAsync(executionId, cancellationToken);
        if (execution is null)
        {
            logger.LogWarning("Execution {Id} disappeared before it could run.", executionId);
            return;
        }

        var script = await scripts.GetByIdAsync(execution.ScriptId, cancellationToken);
        if (script is null)
        {
            execution.MarkFailed($"Script {execution.ScriptId} no longer exists.");
            await executions.UpdateAsync(execution, cancellationToken);
            return;
        }

        execution.MarkRunning();
        await executions.UpdateAsync(execution, cancellationToken);

        try
        {
            var scriptPath = Path.IsPathRooted(script.FilePath)
                ? script.FilePath
                : Path.Combine(_opts.ScriptsDirectory, script.FilePath);

            var parameters = DeserializeParameters(execution.ParametersJson);

            var result = await executor.ExecuteAsync(
                scriptPath,
                parameters,
                TimeSpan.FromSeconds(_opts.TimeoutSeconds),
                cancellationToken);

            if (result.TimedOut)
            {
                execution.MarkCompleted(result.Stdout, $"Timed out after {_opts.TimeoutSeconds}s.", 124);
            }
            else
            {
                execution.MarkCompleted(result.Stdout, result.Stderr, result.ExitCode);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            execution.MarkCancelled();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PowerShell execution {Id} failed.", executionId);
            execution.MarkFailed(ex.Message);
        }

        await executions.UpdateAsync(execution, CancellationToken.None);
    }

    private static IReadOnlyDictionary<string, object?> DeserializeParameters(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, object?>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number when prop.Value.TryGetInt64(out var l) => l,
                    JsonValueKind.Number => prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => prop.Value.ToString(),
                };
            }
            return dict;
        }
        catch (JsonException)
        {
            return new Dictionary<string, object?>();
        }
    }
}
