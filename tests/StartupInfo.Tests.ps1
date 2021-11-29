BeforeAll {
    . ([IO.Path]::Combine($PSScriptRoot, 'common.ps1'))
}

Describe "StartupInfo" {
    It "Gets startupinfo of current process" {
        $actual = Get-StartupInfo
        # Can't really test values for this case, this just verifies it doesn't crash and returns the right object.
        # The tests below do further testing.
        $actual -is [ProcessEx.StartupInfo] | Should -Be $true
    }

    It "Gets startupinfo with custom properties" {
        $reserved2Bytes = [byte[]]@(0, 0, 0, 0) + @([Text.Encoding]::Unicode.GetBytes("test message"))
        $siParams = @{
            Title =  "my title"
            Position = [System.Management.Automation.Host.Coordinates]::new(1, 2)
            WindowSize = [System.Management.Automation.Host.Size]::new(3, 4)
            CountChars = [System.Management.Automation.Host.Size]::new(10, 20)
            FillAttribute = [ProcessEx.ConsoleFill]"BackgroundBlue, BackgroundIntensity"
            Flags = [ProcessEx.StartupInfoFlags]::UntrustedSource
            WindowStyle = [ProcessEx.WindowStyle]::Hide
            Reserved = "reserved 1"
            Reserved2 = $reserved2Bytes
        }
        $si = New-StartupInfo @siparams
        $session = New-ProcessExSession -StartupInfo $si
        try {
            $actual = Invoke-Command $session { Get-StartupInfo }

            $station = [ProcessExTests.Native]::GetProcessWindowStation()
            $desktop = [ProcessExTests.Native]::GetThreadDesktop([ProcessExTests.Native]::GetCurrentThreadId())
            $desktopValue = '{0}\{1}' -f (
                [ProcessExTests.Native]::GetUserObjectName($station),
                [ProcessExTests.Native]::GetUserObjectName($desktop)
            )

            $actual.Desktop | Should -Be $desktopValue
            $actual.Title | Should -Be "my title"
            $actual.Position | Should -Be "1,2"
            $actual.WindowSize | Should -Be "3,4"
            $actual.CountChars | Should -Be "10,20"
            $actual.FillAttribute | Should -Be "BackgroundBlue, BackgroundIntensity"
            $actual.Flags | Should -Be "UseShowWindow, UseSize, UsePosition, UseCountChars, UseFillAttribute, UntrustedSource"
            $actual.ShowWindow | Should -Be "Hide"
            $actual.Reserved | Should -Be "reserved 1"
            [Convert]::ToBase64String($actual.Reserved2) | Should -Be ([Convert]::ToBase64String($reserved2Bytes))
        }
        finally {
            $session | Remove-ProcessExSession
        }
    }

    It "Fails to set stdout with UseHotKey" {
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        try {
            $err = $null
            $actual = New-StartupInfo -Flags UseHotKey -StandardOutput $pipe.SafePipeHandle -ErrorAction SilentlyContinue -ErrorVariable err
            $actual | Should -Be $null
            $err.Count | Should -Be 1
            [string]$err[0] | Should -Be "Cannot set StandardOutput or StandardError with the flags UseHotKey"
        }
        finally {
            $pipe.Dispose()
        }
    }
}
