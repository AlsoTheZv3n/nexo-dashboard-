[CmdletBinding()]
param(
    [Parameter()]
    [string]$ApiBaseUrl = 'http://localhost:5000/api',

    [Parameter()]
    [string]$AccessToken
)

# Collects a handful of host metrics and emits them as a batch payload
# for POST /api/v1/metrics/bulk. When -AccessToken is provided, the script
# posts directly; otherwise it writes the JSON payload to stdout so a caller
# (or a CronJob) can pipe it wherever it needs to go.

function Get-DriveFreePercent {
    Get-PSDrive -PSProvider FileSystem |
        Where-Object { $_.Used -ne $null -and $_.Free -ne $null -and ($_.Used + $_.Free) -gt 0 } |
        ForEach-Object {
            $total = $_.Used + $_.Free
            [PSCustomObject]@{
                Drive   = $_.Name
                Percent = [math]::Round(($_.Free / $total) * 100, 2)
            }
        }
}

function Get-ProcessCount {
    (Get-Process | Measure-Object).Count
}

$timestamp = (Get-Date).ToUniversalTime().ToString("o")
$items = New-Object System.Collections.Generic.List[object]

foreach ($drive in Get-DriveFreePercent) {
    $items.Add([PSCustomObject]@{
        key       = 'host.disk.free_percent'
        value     = $drive.Percent
        timestamp = $timestamp
        tags      = @{ drive = $drive.Drive }
    })
}

$items.Add([PSCustomObject]@{
    key       = 'host.process.count'
    value     = (Get-ProcessCount)
    timestamp = $timestamp
    tags      = @{ host = [Environment]::MachineName }
})

$payload = [PSCustomObject]@{ items = $items.ToArray() }

if ([string]::IsNullOrWhiteSpace($AccessToken)) {
    # Dry-run mode: emit JSON so the Pester test (and CI) can assert shape without needing a running API.
    $payload | ConvertTo-Json -Depth 5
    return
}

$json = $payload | ConvertTo-Json -Depth 5 -Compress
$headers = @{ Authorization = "Bearer $AccessToken" }
Invoke-RestMethod -Method Post -Uri "$ApiBaseUrl/v1/metrics/bulk" -Headers $headers -ContentType 'application/json' -Body $json
