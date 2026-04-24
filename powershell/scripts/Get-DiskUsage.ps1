[CmdletBinding()]
param()

Get-PSDrive -PSProvider FileSystem |
    Where-Object { $_.Used -ne $null -or $_.Free -ne $null } |
    ForEach-Object {
        $used  = if ($_.Used) { [math]::Round($_.Used / 1GB, 2) } else { $null }
        $free  = if ($_.Free) { [math]::Round($_.Free / 1GB, 2) } else { $null }
        $total = if ($used -ne $null -and $free -ne $null) { [math]::Round($used + $free, 2) } else { $null }

        [PSCustomObject]@{
            Name    = $_.Name
            Root    = $_.Root
            UsedGB  = $used
            FreeGB  = $free
            TotalGB = $total
        }
    } |
    ConvertTo-Json -Depth 3
