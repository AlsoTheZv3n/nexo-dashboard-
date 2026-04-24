Describe 'Collect-Metrics (dry-run)' {
    BeforeAll {
        $script:ScriptPath = Join-Path $PSScriptRoot '..\scripts\Collect-Metrics.ps1'
    }

    It 'emits a JSON payload with an items array' {
        $raw = & $script:ScriptPath
        $raw | Should -Not -BeNullOrEmpty
        $obj = $raw | ConvertFrom-Json
        $obj.items | Should -Not -BeNullOrEmpty
        @($obj.items).Count | Should -BeGreaterThan 0
    }

    It 'includes a process count metric' {
        $obj = & $script:ScriptPath | ConvertFrom-Json
        $procRow = @($obj.items) | Where-Object { $_.key -eq 'host.process.count' } | Select-Object -First 1
        $procRow | Should -Not -BeNullOrEmpty
        $procRow.value | Should -BeGreaterThan 0
    }

    It 'each metric has key, value, timestamp' {
        $obj = & $script:ScriptPath | ConvertFrom-Json
        foreach ($item in @($obj.items)) {
            $item.key | Should -Not -BeNullOrEmpty
            $item.value | Should -Not -BeNullOrEmpty
            $item.timestamp | Should -Not -BeNullOrEmpty
        }
    }
}
