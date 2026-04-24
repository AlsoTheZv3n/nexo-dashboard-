Describe 'Get-SystemHealth' {
    BeforeAll {
        $script:ScriptPath = Join-Path $PSScriptRoot '..\scripts\Get-SystemHealth.ps1'
    }

    It 'returns JSON with a Status field' {
        $raw = & $script:ScriptPath
        $raw | Should -Not -BeNullOrEmpty
        $obj = $raw | ConvertFrom-Json
        $obj.Status | Should -BeIn @('Ok', 'Warning')
    }

    It 'reports Warning when the threshold is unreachable' {
        $raw = & $script:ScriptPath -MinFreeGB 999999999
        $obj = $raw | ConvertFrom-Json
        $obj.Status | Should -Be 'Warning'
        $obj.BelowCount | Should -BeGreaterThan 0
    }

    It 'respects the MinFreeGB parameter' {
        $raw = & $script:ScriptPath -MinFreeGB 1
        $obj = $raw | ConvertFrom-Json
        $obj.MinFreeGB | Should -Be 1
    }
}
