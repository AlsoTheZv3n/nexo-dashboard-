Describe 'Get-DiskUsage' {
    BeforeAll {
        $script:ScriptPath = Join-Path $PSScriptRoot '..\scripts\Get-DiskUsage.ps1'
    }

    It 'returns at least one drive' {
        $raw = & $script:ScriptPath
        $obj = $raw | ConvertFrom-Json
        # ConvertFrom-Json returns a single object for a 1-element array, normalise to array:
        @($obj).Count | Should -BeGreaterThan 0
    }

    It 'each drive has a Name property' {
        $raw = & $script:ScriptPath
        $obj = @( $raw | ConvertFrom-Json )
        $obj | ForEach-Object { $_.Name | Should -Not -BeNullOrEmpty }
    }
}
