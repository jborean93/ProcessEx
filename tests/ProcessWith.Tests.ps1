BeforeAll {
    . ([IO.Path]::Combine($PSScriptRoot, 'common.ps1'))
}

Describe "Start-ProcessWith" {
    BeforeAll {
        $domain = $env:COMPUTERNAME
        $username = "ProcessEx-Test"
        $password = "Password123!"

        $userParams = @{
            Name = $username
            Password = (ConvertTo-SecureString -AsPlainText -Force -String $password)
            Description = "Test user for ProcessEx with higher privileges"
            PasswordNeverExpires = $true
            UserMayNotChangePassword = $true
            GroupMembership = "Administrators"
        }
        $user = New-LocalAccount @userParams

        # PowerShell runs in lockdown mode if it cannot access the temp path. The temp path will either be the
        # callers temp path or C:\Windows\TEMP depending on the environment settings so apply this to both.
        ([IO.Path]::GettempPath()), 'C:\Windows\TEMP' | ForEach-Object {
            $acl = Get-Acl -LiteralPath $_
            $acl.AddAccessRule($acl.AccessRuleFactory($user,
                [System.Security.AccessControl.FileSystemRights]::FullControl,
                $false,
                [System.Security.AccessControl.InheritanceFlags]"ContainerInherit, ObjectInherit",
                [System.Security.AccessControl.PropagationFlags]::None,
                [System.Security.AccessControl.AccessControlType]::Allow))
            Set-Acl -LiteralPath $_ -AclObject $acl
        }

        $credential = [PSCredential]::new($userParams.Name, $userParams.Password)
        $token = [ProcessExTests.Native]::LogonUser($username, $domain, $password, 2, 0)
    }
    AfterAll {
        $token.Dispose()

        ([IO.Path]::GettempPath()), 'C:\Windows\TEMP' | ForEach-Object {
            $acl = Get-Acl -LiteralPath $_
            $acl.PurgeAccessRules($user)
            Set-Acl -LiteralPath $_ -AclObject $acl
        }

        Remove-LocalAccount -Account $user
    }

    AfterEach {
        $profileDir = "C:\Users\$($userParams.Name)"
        if (Test-Path -Path $profileDir) {
            # Just in case there is something that wasn't disposed that holds a handle in the profile run the GC
            # explicitly and wait for any pending finalizers to finish disposing the data before deleting the profile.
            [GC]::Collect()
            [GC]::WaitForPendingFinalizers()

            [ProcessExTests.Native]::DeleteProfile($user.Value, [NullString]::Value, [NullString]::Value)

            # DeleteProfile does not always delete everything, check manually
            if (Test-Path -Path $profileDir) {
                Remove-Item -Path $profileDir -Force -Recurse -ErrorAction SilentlyContinue
            }
        }
    }

    It "Starts a new process without passthru" {
        $actual = Start-ProcessWith cmd.exe /c echo hi -Token $token
        $actual | Should -Be $null
    }

    It "Starts a new process with passthru" {
        $actual = Start-ProcessWith powershell.exe sleep 1 -PassThru -Token $token
        $actual -is ([ProcessEx.ProcessInfo]) | Should -Be $true
        $actual.ExitCode | Should -Be $null
        $actual | Wait-Process
        $actual.ExitCode | Should -Be 0
    }

    It "Waits until process is finished" {
        $actual = Start-ProcessWith powershell.exe sleep 1 -PassThru -Wait -Token $token
        $actual -is ([ProcessEx.ProcessInfo]) | Should -Be $true
        $actual.ExitCode | Should -Be 0
    }

    It "Fails to create process with bad path" {
        $err = $null
        Start-ProcessWith bad.exe -Token $token -ErrorAction SilentlyContinue -ErrorVariable err
        $err.Count | Should -Be 1
        [string]$err[0] | Should -BeLike 'Failed to create process *'
    }

    It "Runs normally - <Case>" -TestCases @(
        @{ Case = "Token" },
        @{ Case = "Credential" }
    ) {
        param ($Case)

        $params = @{ "$Case" = (Get-Variable -Name $Case -ValueOnly) }

        $session = New-ProcessWithSession @params
        try {
            $actual = Invoke-Command $session {
                [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
            }

            $actual | Should -Be $user.Value
        }
        finally {
            $session | Remove-ProcessExSession
        }
    }

    It "Runs with limited token" -Skip:(Get-ProcessPrivilege -Name SeTcbPrivilege).IsRemoved {
        Enable-ProcessPrivilege -Name SeTcbPrivilege

        $session = $null
        $disposeToken = $false
        try {
            $elevationType = Get-TokenElevationType -Token $token

            $tokenToUse = if ($elevationType -eq 1) {
                return  # No split token, cannot test this
            }
            elseif ($elevationType -eq 2) {
                # Have the full token, get the linked limited token
                Get-TokenLinkedToken -Token $token
                $disposeToken = $true
            }
            else {
                $token
            }

            $session = New-ProcessWithSession -Token $tokenToUse
            $actual = Invoke-Command $session {

                $principal = [System.Security.Principal.WindowsPrincipal](
                    [System.Security.Principal.WindowsIdentity]::GetCurrent())
                $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
            }

            $actual | Should -Be $false
        }
        finally {
            if ($session) { $session | Remove-ProcessExSession }
            if ($disposeToken) { $tokenToUse.Dispose() }
        }
    }

    It "Runs with elevated token" -Skip:(Get-ProcessPrivilege -Name SeTcbPrivilege).IsRemoved {
        Enable-ProcessPrivilege -Name SeTcbPrivilege

        $session = $null
        $disposeToken = $false
        try {
            $elevationType = Get-TokenElevationType -Token $token

            $tokenToUse = if ($elevationType -eq 1) {
                return  # No split token, cannot test this
            }
            elseif ($elevationType -eq 3) {
                # Have the limited token, get the linked full token
                Get-TokenLinkedToken -Token $token
                $disposeToken = $true
            }
            else {
                $token
            }

            $session = New-ProcessWithSession -Token $tokenToUse
            $actual = Invoke-Command $session {

                $principal = [System.Security.Principal.WindowsPrincipal](
                    [System.Security.Principal.WindowsIdentity]::GetCurrent())
                $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
            }

            $actual | Should -Be $true
        }
        finally {
            if ($session) { $session | Remove-ProcessExSession }
            if ($disposeToken) { $tokenToUse.Dispose() }
        }
    }

    It "Runs as SYSTEM" {
        $systemSid = [System.Security.Principal.SecurityIdentifier]::new(
            [System.Security.Principal.WellKnownSidType]::LocalSystemSid, $null)
        $systemName = $systemSid.Translate([System.Security.Principal.NTAccount]).Value

        $actual = Get-Process -IncludeUserName | Where-Object UserName -eq $systemName | ForEach-Object {
            $systemToken = Get-ProcessToken -Process $_ -Access Duplicate, Impersonate, Query -ErrorAction SilentlyContinue
            if (-not $systemToken) { return }

            # AdjustDefault, AdjustSessionId aren't granted to admins but only SYSTEM by default. by impersonating
            # system we can then reopen the token with the required access.
            [ProcessExTests.Native]::ImpersonateLoggedOnUser($systemToken)
            try {
                $systemToken = Get-ProcessToken -Process $_ -Access AdjustDefault, AdjustSessionId, AssignPrimary, Duplicate, Query
                $systemToken = [ProcessExTests.Native]::DuplicateTokenEx($systemToken,
                    0,
                    "SecurityImpersonation",
                    "Primary")
            }
            finally {
                [ProcessExTests.Native]::RevertToSelf()
            }

            $session = $null
            try {
                $session = New-ProcessWithSession -Token $systemToken
                Invoke-Command $session {
                    [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
                }
            }
            finally {
                if ($session) { $session | Remove-ProcessExSession }
                $systemToken.Dispose()
            }
        } | Select-Object -First 1
        $actual | Should -Be $systemSid.Value
    }

    It "Inherits current working directory - <Case>" -TestCases @(
        @{ Case = "Token" },
        @{ Case = "Credential" }
    ) {
        param ($Case)

        $params = @{ "$Case" = (Get-Variable -Name $Case -ValueOnly) }

        Push-Location $TestDrive
        try {
            $path = (Get-Item -LiteralPath $TestDrive).FullName
            $session = New-ProcessWithSession @params
            try {
                $actual = Invoke-Command $session { [System.Environment]::CurrentDirectory }
            }
            finally {
                $session | Remove-ProcessExSession
            }

            $actual | Should -Be $path
        }
        finally {
            Pop-Location
        }
    }

    It "Sets working directory - <Case>" -TestCases @(
        @{ Case = "Token" },
        @{ Case = "Credential" }
    ) {
        param ($Case)

        $params = @{ "$Case" = (Get-Variable -Name $Case -ValueOnly) }

        $path = (Get-Item -LiteralPath $TestDrive).FullName
        $session = New-ProcessWithSession -WorkingDirectory $path @params
        try {
            $actual = Invoke-Command $session { [System.Environment]::CurrentDirectory }
        }
        finally {
            $session | Remove-ProcessExSession
        }

        $actual | Should -Be $path
    }

    It "Uses custom environment - <Case>" -TestCases @(
        @{ Case = "Token" },
        @{ Case = "Credential" }
    ) {
        param ($Case)

        $params = @{ "$Case" = (Get-Variable -Name $Case -ValueOnly) }

        $env = @{
            Testing = "abc"
            TMP = $env:TMP
            SystemRoot = $Env:SystemRoot
        }
        $session = New-ProcessWithSession -Environment $env @params
        try {
            $actual = Invoke-Command $session { [System.Environment]::GetEnvironmentVariables() }
        }
        finally {
            $session | Remove-ProcessExSession
        }

        $actual.Testing | Should -Be "abc"
        $actual.TMP | Should -Be $env:TMP
    }

    It "Redirects stdio - <Case>" -TestCases @(
        @{ Case = "Token" },
        @{ Case = "Credential" }
    ) {
        param ($Case)

        $stdout = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $stderr = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $stdin = [System.IO.Pipes.AnonymousPipeServerStream]::new("Out", "Inheritable")
        try {
            $si = New-StartupInfo -StandardOutput $stdout.ClientSafePipeHandle -StandardError $stderr.ClientSafePipeHandle -StandardInput $stdin.ClientSafePipeHandle
            $procParams = @{
                FilePath = 'powershell.exe'
                ArgumentList = '-Command', '-'
                StartupInfo = $si
                "$Case" = (Get-Variable -Name $Case -ValueOnly)
            }
            Start-ProcessWith @procParams
            $stdout.DisposeLocalCopyOfClientHandle()
            $stderr.DisposeLocalCopyOfClientHandle()
            $stdin.DisposeLocalCopyOfClientHandle()

            $stdinWriter = [IO.StreamWriter]::new($stdin)
            $stdoutReader = [IO.StreamReader]::new($stdout)
            $stderrReader = [IO.StreamReader]::new($stderr)

            $stdinWriter.WriteLine("[Console]::Out.WriteLine('stdout')")
            $stdinWriter.WriteLine("[Console]::Error.WriteLine('stderr')")
            $stdinWriter.WriteLine("exit")
            $stdinWriter.Dispose()

            $stdoutActual = $stdoutReader.ReadToEnd()
            $stderrActual = $stderrReader.ReadToEnd()

            $stdoutActual.Trim() | Should -Be "stdout"
            $stderrActual.Trim() | Should -Be "stderr"
        }
        finally {
            $stdout.Dispose()
            $stderr.Dispose()
            $stdin.Dispose()
        }
    }

    It "Sets a custom desktop - <Case>" -TestCases @(
        @{ Case = "Token" },
        @{ Case = "Credential" }
    ) {
        param ($Case)

        $params = @{ "$Case" = (Get-Variable -Name $Case -ValueOnly) }

        $desktopName = "ProcessExDesktop"
        $desktop = [ProcessExTests.Native]::CreateDesktopEx($desktopName, "NONE",
            [ProcessEx.Security.DesktopAccessRights]::AllAccess, 128)
        try {
            $si = New-StartupInfo -Desktop $desktopName
            $session = New-ProcessWithSession -StartupInfo $si @params
            try {
                $actual = Invoke-Command $session {
                    $ErrorActionPreference = 'Stop'

                    $station = [ProcessExTests.Native]::GetProcessWindowStation()
                    $desktop = [ProcessExTests.Native]::GetThreadDesktop([ProcessExTests.Native]::GetCurrentThreadId())
                    '{0}\{1}' -f (
                        [ProcessExTests.Native]::GetUserObjectName($station),
                        [ProcessExTests.Native]::GetUserObjectName($desktop)
                    )
                }

                $currentStation = [ProcessExTests.Native]::GetProcessWindowStation()
                $stationName = [ProcessExTests.Native]::GetUserObjectName($currentStation)

                $actual | Should -Be "$stationName\$desktopName"
            }
            finally {
                $session | Remove-ProcessExSession
            }
        }
        finally {
            $desktop.Dispose()
        }
    }

    It "Sets explicit desktop same as caller - <Case>" -TestCases @(
        @{ Case = "Token" },
        @{ Case = "Credential" }
    ) {
        param ($Case)

        $params = @{ "$Case" = (Get-Variable -Name $Case -ValueOnly) }

        $desktop = [ProcessExTests.Native]::GetThreadDesktop([ProcessExTests.Native]::GetCurrentThreadId())
        $desktopName = [ProcessExTests.Native]::GetUserObjectName($desktop)

        $si = New-StartupInfo -Desktop $desktopName
        $session = New-ProcessWithSession -StartupInfo $si @params
        try {
            $actual = Invoke-Command $session {
                $ErrorActionPreference = 'Stop'

                $station = [ProcessExTests.Native]::GetProcessWindowStation()
                $desktop = [ProcessExTests.Native]::GetThreadDesktop([ProcessExTests.Native]::GetCurrentThreadId())
                '{0}\{1}' -f (
                    [ProcessExTests.Native]::GetUserObjectName($station),
                    [ProcessExTests.Native]::GetUserObjectName($desktop)
                )
            }

            $currentStation = [ProcessExTests.Native]::GetProcessWindowStation()
            $stationName = [ProcessExTests.Native]::GetUserObjectName($currentStation)

            $actual | Should -Be "$stationName\$desktopName"
        }
        finally {
            $session | Remove-ProcessExSession
        }
    }

    It "Set explicit station\desktop with station same as caller - <Case>" -TestCases @(
        @{ Case = "Token" },
        @{ Case = "Credential" }
    ) {
        param ($Case)

        $params = @{ "$Case" = (Get-Variable -Name $Case -ValueOnly) }

        $station = [ProcessExTests.Native]::GetProcessWindowStation()
        $stationName = [ProcessExTests.Native]::GetUserObjectName($station)

        $desktopName = "ProcessExDesktop"
        $desktop = [ProcessExTests.Native]::CreateDesktopEx($desktopName, "NONE",
            [ProcessEx.Security.DesktopAccessRights]::AllAccess, 128)
        try {
            $si = New-StartupInfo -Desktop "$stationName\$desktopName"
            $session = New-ProcessWithSession -StartupInfo $si @params
            try {
                $actual = Invoke-Command $session {
                    $ErrorActionPreference = 'Stop'

                    $station = [ProcessExTests.Native]::GetProcessWindowStation()
                    $desktop = [ProcessExTests.Native]::GetThreadDesktop([ProcessExTests.Native]::GetCurrentThreadId())
                    '{0}\{1}' -f (
                        [ProcessExTests.Native]::GetUserObjectName($station),
                        [ProcessExTests.Native]::GetUserObjectName($desktop)
                    )
                }

                $actual | Should -Be "$stationName\$desktopName"
            }
            finally {
                $session | Remove-ProcessExSession
            }
        }
        finally {
            $desktop.Dispose()
        }
    }

    It "Sets a custom station and desktop - <Case>" -TestCases @(
        @{ Case = "Token" },
        @{ Case = "Credential" }
    ) {
        param ($Case)

        $params = @{ "$Case" = (Get-Variable -Name $Case -ValueOnly) }

        $stationName = "ProcessExStation"
        $desktopName = "ProcessExDesktop"

        $resetStation = $false
        $currentStation = $station = $desktop = $null
        try {
            $currentStation = [ProcessExTests.Native]::GetProcessWindowStation()
            $currentStationName = [ProcessExTests.Native]::GetUserObjectName($currentStation)

            $station = [ProcessExTests.Native]::CreateWindowStation($stationName, "NONE",
                [ProcessEx.Security.StationAccessRights]::AllAccess)
            [ProcessExTests.Native]::SetProcessWindowStation($station)
            $resetStation = $true

            $desktop = [ProcessExTests.Native]::CreateDesktopEx($desktopName, "NONE",
                [ProcessEx.Security.DesktopAccessRights]::AllAccess, 128)

            [ProcessExTests.Native]::SetProcessWindowStation($currentStation)
            $resetStation = $false

            $si = New-StartupInfo -Desktop "$stationName\$desktopName"
            $session = New-ProcessWithSession -StartupInfo $si @params
            try {
                $actual = Invoke-Command $session {
                    $ErrorActionPreference = 'Stop'

                    $station = [ProcessExTests.Native]::GetProcessWindowStation()
                    $desktop = [ProcessExTests.Native]::GetThreadDesktop([ProcessExTests.Native]::GetCurrentThreadId())
                    '{0}\{1}' -f (
                        [ProcessExTests.Native]::GetUserObjectName($station),
                        [ProcessExTests.Native]::GetUserObjectName($desktop)
                    )
                }

                $actual | Should -Be $si.Desktop

                # Verify Start-ProcessEx didn't change the station when editing the desktop ACLs
                $processStation = [ProcessExTests.Native]::GetProcessWindowStation()
                $processStationName = [ProcessExTests.Native]::GetUserObjectName($processStation)
                $processStationName | Should -Be $currentStationName
            }
            finally {
                $session | Remove-ProcessExSession
            }
        }
        finally {
            if ($resetStation) { [ProcessExTests.Native]::SetProcessWindowStation($currentStation) }
            if ($currentStation) { $currentStation.Dispose() }
            if ($station) { $station.Dispose() }
            if ($desktop) { $desktop.Dispose() }
        }
    }

    It "Sets explicit station and desktop same as caller - <Case>" -TestCases @(
        @{ Case = "Token" },
        @{ Case = "Credential" }
    ) {
        param ($Case)

        $params = @{ "$Case" = (Get-Variable -Name $Case -ValueOnly) }

        $station = [ProcessExTests.Native]::GetProcessWindowStation()
        $stationName = [ProcessExTests.Native]::GetUserObjectName($station)

        $desktop = [ProcessExTests.Native]::GetThreadDesktop([ProcessExTests.Native]::GetCurrentThreadId())
        $desktopName = [ProcessExTests.Native]::GetUserObjectName($desktop)

        $si = New-StartupInfo -Desktop "$stationName\$desktopName"
        $session = New-ProcessWithSession -StartupInfo $si @params
        try {
            $actual = Invoke-Command $session {
                $ErrorActionPreference = 'Stop'

                $station = [ProcessExTests.Native]::GetProcessWindowStation()
                $desktop = [ProcessExTests.Native]::GetThreadDesktop([ProcessExTests.Native]::GetCurrentThreadId())
                '{0}\{1}' -f (
                    [ProcessExTests.Native]::GetUserObjectName($station),
                    [ProcessExTests.Native]::GetUserObjectName($desktop)
                )
            }

            $actual | Should -Be "$stationName\$desktopName"
        }
        finally {
            $session | Remove-ProcessExSession
        }
    }

    It "Runs with NetCredentialsOnly" {
        $cred = [PSCredential]::new("fake-user@REALM.COM",
            (ConvertTo-SecureString -AsPlainText -Force -String "password"))

        $session = New-ProcessWithSession -Credential $cred -NetCredentialsOnly
        try {
            $actual = Invoke-Command $session {
                [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
            }

            # The local actions run as the current user, network credentials use the new creds.
            $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
            $actual | Should -Be $currentUser
        }
        finally {
            $session | Remove-ProcessExSession
        }
    }

    It "Runs with Profile - <Case>" -TestCases @(
        @{ Case = "Token" },
        @{ Case = "Credential" }
    ) {
        param ($Case)

        $params = @{ "$Case" = (Get-Variable -Name $Case -ValueOnly) }

        $session = New-ProcessWithSession @params
        try {
            $actual = Invoke-Command $session {
                $ErrorActionPreference = 'Stop'

                $env:USERPROFILE
            }
        }
        finally {
            $session | Remove-ProcessExSession
        }

        # Without loading the profile it will default to the default users dir
        $actual | Should -Be "C:\Users\Default"

        $session = New-ProcessWithSession @params -WithProfile
        try {
            $actual = Invoke-Command $session {
                $ErrorActionPreference = 'Stop'

                $env:USERPROFILE
            }
        }
        finally {
            $session | Remove-ProcessExSession
        }

        $actual | Should -Be "C:\Users\ProcessEx-Test"
    }

    It "Fails with suspend and -Wait" {
        $err = $null
        Start-ProcessWith pwsh -CreationFlags Suspended -Wait -Token $token -ErrorAction SilentlyContinue -ErrorVariable err
        $err.Count | Should -Be 1
        [string]$err[0] | Should -Be "Cannot use -Wait with -CreationFlags Suspended"
    }

    It "Fails with inherited handles" {
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        try {
            $err = $null
            $si = New-StartupInfo -InheritedHandle $pipe.SafePipeHandle
            Start-ProcessWith pwsh -StartupInfo $si -Token $token -ErrorAction SilentlyContinue -ErrorVariable err
            $err.Count | Should -Be 1
            [string]$err[0] | Should -Be "Start-ProcessWith cannot be used with InheritedHandles"
        }
        finally {
            $pipe.Dispose()
        }
    }

    It "Fails with parent process" {
        $si = New-StartupInfo -ParentProcess $pid
        $err = $null
        Start-ProcessWith pwsh -StartupInfo $si -Token $token -ErrorAction SilentlyContinue -ErrorVariable err
        $err.Count | Should -Be 1
        [string]$err[0] | Should -Be "Start-ProcessWith cannot be used with ParentProcess"
    }

    It "Fails with ConPTY" {
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        try {
            $err = $null
            $si = New-StartupInfo -ConPTY $pipe.SafePipeHandle
            Start-ProcessWith pwsh -StartupInfo $si -Token $token -ErrorAction SilentlyContinue -ErrorVariable err
            $err.Count | Should -Be 1
            [string]$err[0] | Should -Be "Start-ProcessWith cannot be used with ConPTY"
        }
        finally {
            $pipe.Dispose()
        }
    }
}
