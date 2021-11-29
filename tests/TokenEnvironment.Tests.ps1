BeforeAll {
    . ([IO.Path]::Combine($PSScriptRoot, 'common.ps1'))
}

Describe "Get-TokenEnvironment" {
    It "Gets env var for current process token" {
        $actual = Get-TokenEnvironment

        $actual -is [System.Collections.IDictionary] | Should -Be $true
        $actual.Count | Should -BeGreaterThan 0
    }

    It "Gets env var for new token" {
        $proc = Get-ProcessEx -Id $pid

        $access = [System.Security.Principal.TokenAccessLevels]::Query
        $token = [ProcessExTests.Native]::OpenProcessToken($proc.Process, $access)
        try {
            $actual = Get-TokenEnvironment -Token $token
            $actual -is [System.Collections.IDictionary] | Should -Be $true
            $actual.Count | Should -BeGreaterThan 0
        }
        finally {
            $token.Dispose()
        }
    }

    It "Fails to get env for invalid access" {
        $proc = Get-ProcessEx -Id $pid

        $access = [System.Security.Principal.TokenAccessLevels]::QuerySource
        $token = [ProcessExTests.Native]::OpenProcessToken($proc.Process, $access)
        try {
            $err = $null
            $actual = Get-TokenEnvironment -Token $token -ErrorAction SilentlyContinue -ErrorVariable err
            $actual | Should -Be $null
            $err.Count | Should -Be 1
            [string]$err[0] | Should -BeLike "Failed to get token environment block *"
        }
        finally {
            $token.Dispose()
        }
    }
}
