Describe 'Test-NetworkConnectivity' {
    BeforeAll {
        $script:ScriptPath = Join-Path $PSScriptRoot '..\scripts\Test-NetworkConnectivity.ps1'
    }

    It 'returns Reachable=false for a guaranteed-unreachable target with a short timeout' {
        $raw = & $script:ScriptPath -Target '192.0.2.1:1' -TimeoutMs 200
        $obj = $raw | ConvertFrom-Json
        $obj.Reachable | Should -Be $false
    }

    It 'accepts a port in the target string' {
        $raw = & $script:ScriptPath -Target 'example.invalid:1234' -TimeoutMs 200
        $obj = $raw | ConvertFrom-Json
        $obj.Port | Should -Be 1234
    }
}
