BeforeAll {
    . ([IO.Path]::Combine($PSScriptRoot, 'common.ps1'))
}

Describe "Get-ProcessEnvironment" {
    It "Gets environment for current process" {
        $expected = [System.Environment]::GetEnvironmentVariables()

        $actual = Get-ProcessEnvironment
        $actual -is [System.Collections.IDictionary] | Should -Be $true
        $actual.Count | Should -Be $expected.Count
    }

    It "Gets environment using pid" {
        $actual = Get-ProcessEnvironment -Process $pid
        $actual -is [System.Collections.IDictionary] | Should -Be $true
        $actual.Count | Should -BeGreaterThan 0
    }

    It "Gets environment using Process" {
        $actual = Get-ProcessEnvironment -Process (Get-Process -Id $pid)
        $actual -is [System.Collections.IDictionary] | Should -Be $true
        $actual.Count | Should -BeGreaterThan 0
    }

    It "Gets environment using ProcessInfo" {
        $expected = [System.Environment]::GetEnvironmentVariables()
        $proc = Start-ProcessEx powershell -PassThru
        try {
            $actual = Get-ProcessEnvironment -Process $proc
            $actual -is [System.Collections.IDictionary] | Should -Be $true
            $actual.Count | Should -Be $expected.Count
        }
        finally {
            $proc | Stop-Process -Force
        }
    }

    It "Fails to get environment with invalid pid" {
        $err = $null
        $actual = Get-ProcessEnvironment -Id 0 -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -Be $null
        $err.Count | Should -Be 1
        [string]$err[0] | Should -BeLike "Failed to open process handle *"
    }
}
