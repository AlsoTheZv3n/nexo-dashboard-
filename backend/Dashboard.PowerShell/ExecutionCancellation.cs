using System.Collections.Concurrent;

namespace Dashboard.PowerShell;

/// <summary>
/// Singleton registry of per-execution CancellationTokenSources so the API layer
/// can cooperatively cancel an in-flight PowerShell run. Stored in-memory and
/// bound to the owning process: cluster-wide cancel requires the Worker-based
/// architecture (docs/02-ARCHITECTURE.md §3.3 Variante B).
/// </summary>
public sealed class ExecutionCancellation
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _sources = new();

    public CancellationTokenSource Register(Guid executionId, CancellationToken linkedTo)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(linkedTo);
        _sources[executionId] = cts;
        return cts;
    }

    public void Remove(Guid executionId)
    {
        if (_sources.TryRemove(executionId, out var cts))
        {
            cts.Dispose();
        }
    }

    /// <summary>Returns true if the execution was known locally and has been signalled.</summary>
    public bool Cancel(Guid executionId)
    {
        if (_sources.TryGetValue(executionId, out var cts) && !cts.IsCancellationRequested)
        {
            cts.Cancel();
            return true;
        }
        return false;
    }
}
