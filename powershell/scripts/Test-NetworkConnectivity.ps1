[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Target,

    [Parameter()]
    [int]$TimeoutMs = 2000
)

# Synchronous single-shot TCP reachability test with a hard timeout. Avoids ICMP so it works
# in environments where ping is disabled. Never use $Host — it is a read-only automatic variable.
try {
    $uri = [System.Uri]::new("tcp://$Target")
    $hostName = $uri.Host
    $port = if ($uri.Port -gt 0) { $uri.Port } else { 443 }
} catch {
    # Fall back: Target may already be "host:port"
    $parts = $Target -split ':'
    $hostName = $parts[0]
    $port = if ($parts.Length -gt 1) { [int]$parts[1] } else { 443 }
}

$client = New-Object System.Net.Sockets.TcpClient
$reachable = $false
$completed = $false
try {
    $task = $client.ConnectAsync($hostName, $port)
    $completed = $task.Wait($TimeoutMs)
    $reachable = $completed -and $client.Connected
} catch {
    # DNS resolution failures, refused connections, etc. — surfaced as Reachable=false.
    $reachable = $false
} finally {
    $client.Close()
}

[PSCustomObject]@{
    Target    = $Target
    Host      = $hostName
    Port      = $port
    Reachable = $reachable
    TimedOut  = -not $completed
} | ConvertTo-Json -Depth 2
