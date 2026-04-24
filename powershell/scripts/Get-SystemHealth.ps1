[CmdletBinding()]
param(
    [Parameter()]
    [int]$MinFreeGB = 10
)

# Returns an object describing system health. "Ok" when every drive exceeds MinFreeGB.
# Small, cross-platform-safe surface so it can run on Windows CI and Ubuntu containers alike.

function Get-DriveFreeGB {
    Get-PSDrive -PSProvider FileSystem |
        Where-Object { $_.Used -ne $null -and $_.Free -ne $null } |
        ForEach-Object {
            [PSCustomObject]@{
                Name   = $_.Name
                FreeGB = [math]::Round($_.Free / 1GB, 2)
            }
        }
}

$drives = Get-DriveFreeGB
$belowThreshold = @($drives | Where-Object { $_.FreeGB -lt $MinFreeGB })
$status = if ($belowThreshold.Count -eq 0) { "Ok" } else { "Warning" }

[PSCustomObject]@{
    Status     = $status
    CheckedAt  = (Get-Date).ToUniversalTime().ToString("o")
    Drives     = $drives
    BelowCount = $belowThreshold.Count
    MinFreeGB  = $MinFreeGB
} | ConvertTo-Json -Depth 4
