BeforeAll {
    . ([IO.Path]::Combine($PSScriptRoot, 'common.ps1'))
}

Describe "Get-ProcessEx" {
    BeforeAll {
        $pwshNative = (Get-Command -Name "powershell.exe" -CommandType Application).Path
        if ([Environment]::Is64BitProcess) {
            $pwsh32 = "C:\Windows\SysWow64\WindowsPowerShell\v1.0\powershell.exe"
            $pwsh64 = $pwshNative
        }
        else {
            $pwsh32 = $pwshNative
            $pwsh64 = "C:\Windows\SysNative\WindowsPowerShell\v1.0\powershell.exe"
        }
    }

    It "Gets process using pid" {
        $actual = Get-ProcessEx -Id $pid
        $actual -is [ProcessEx.ProcessInfo] | Should -Be $true
        $actual.ProcessId | Should -Be $pid
        $actual.Process -is [System.Runtime.InteropServices.SafeHandle] | Should -Be $true
    }

    It "Gets process with Process" {
        $actual = Get-ProcessEx -Id (Get-Process -Id $pid)
        $actual -is [ProcessEx.ProcessInfo] | Should -Be $true
        $actual.ProcessId | Should -Be $pid
        $actual.Process -is [System.Runtime.InteropServices.SafeHandle] | Should -Be $true
    }

    It "Gets process with ProcessInfo" {
        $exePath = (Get-Command -Name powershell.exe -CommandType Application).Path
        $procParams = @{
            CommandLine = "pwsh.exe C:\Program Files\file\\"" KEY=""value test"" {""json"": ""value""}"
            ApplicationName = $exePath
            CreationFlags = [ProcessEx.CreationFlags]::Suspended
            Environment = @{
                AAA = "aaa"
                ZZZ = "zzz"
            }
            PassThru = $true
        }

        $proc = Start-ProcessEx @procParams
        try {
            $actual = Get-ProcessEx -Id $proc
            $actual.ProcessId | Should -Be $proc.Id
            $actual.Id | Should -Be $proc.Id
            if ([Environment]::Is64BitProcess) {
                $actual.Executable | Should -Be  $exePath
            }
            else {
                $actual.Executable | Should -Be "C:\Windows\SysWow64\WindowsPowerShell\v1.0\powershell.exe"
            }
            $actual.CommandLine | Should -Be $procParams.CommandLine
            $actual.Environment.Count | Should -Be 2
            $actual.Environment.AAA | Should -Be "aaa"
            $actual.Environment.ZZZ | Should -Be "zzz"
        }
        finally {
            $proc | Stop-Process -Force
        }
    }

    It "Fails to open missing process" {
        $err = $null
        $actual = Get-ProcessEx -Id 0 -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -Be $null
        $err.Count | Should -Be 1
        [string]$err[0] | Should -BeLike "Failed to open process handle *"
    }

    It "Gets environment and command line x64 -> x64" -Skip:(-not [Environment]::Is64BitOperatingSystem) {
        $cmdLine = "pwsh.exe C:\Program Files\file\\"" KEY=""value test"" {""json"": ""value""}"

        $session = New-ProcessExSession -FilePath $pwsh64
        try {
            $actual = Invoke-Command $session {
                $procParams = @{
                    CommandLine = $args[0]
                    ApplicationName = $args[1]
                    CreationFlags = [ProcessEx.CreationFlags]::Suspended
                    Environment = @{
                        AAA = "aaa"
                        ZZZ = "zzz"
                    }
                    PassThru = $true
                }

                $proc = Start-ProcessEx @procParams
                try {
                    Get-ProcessEx -Process $proc
                }
                finally {
                    $proc | Stop-Process -Force
                }

            } -ArgumentList $cmdLine, $pwshNative

            $actual.Executable | Should -Be  $pwshNative
            $actual.CommandLine | Should -Be $cmdLine
            $actual.Environment.Count | Should -Be 2
            $actual.Environment.AAA | Should -Be "aaa"
            $actual.Environment.ZZZ | Should -Be "zzz"
        }
        finally {
            $session | Remove-ProcessExSession
        }
    }

    It "Gets environment and command line x64 -> x86" -Skip:(-not [Environment]::Is64BitOperatingSystem) {
        $cmdLine = "pwsh.exe C:\Program Files\file\\"" KEY=""value test"" {""json"": ""value""}"

        $session = New-ProcessExSession -FilePath $pwsh64
        try {
            $actual = Invoke-Command $session {
                $procParams = @{
                    CommandLine = $args[0]
                    ApplicationName = $args[1]
                    CreationFlags = [ProcessEx.CreationFlags]::Suspended
                    Environment = @{
                        AAA = "aaa"
                        ZZZ = "zzz"
                    }
                    PassThru = $true
                }

                $proc = Start-ProcessEx @procParams
                try {
                    Get-ProcessEx -Process $proc
                }
                finally {
                    $proc | Stop-Process -Force
                }

            } -ArgumentList $cmdLine, $pwsh32

            $actual.Executable | Should -Be  $pwsh32
            $actual.CommandLine | Should -Be $cmdLine
            $actual.Environment.Count | Should -Be 2
            $actual.Environment.AAA | Should -Be "aaa"
            $actual.Environment.ZZZ | Should -Be "zzz"
        }
        finally {
            $session | Remove-ProcessExSession
        }
    }

    It "Gets environment and command line x86 -> x86" {
        $cmdLine = "pwsh.exe C:\Program Files\file\\"" KEY=""value test"" {""json"": ""value""}"

        $session = New-ProcessExSession -FilePath $pwsh32
        try {
            $actual = Invoke-Command $session {
                $procParams = @{
                    CommandLine = $args[0]
                    ApplicationName = $args[1]
                    CreationFlags = [ProcessEx.CreationFlags]::Suspended
                    Environment = @{
                        AAA = "aaa"
                        ZZZ = "zzz"
                    }
                    PassThru = $true
                }

                $proc = Start-ProcessEx @procParams
                try {
                    Get-ProcessEx -Process $proc
                }
                finally {
                    $proc | Stop-Process -Force
                }

            } -ArgumentList $cmdLine, $pwsh32

            $actual.Executable | Should -Be "C:\Windows\SysWow64\WindowsPowerShell\v1.0\powershell.exe"
            $actual.CommandLine | Should -Be $cmdLine
            $actual.Environment.Count | Should -Be 2
            $actual.Environment.AAA | Should -Be "aaa"
            $actual.Environment.ZZZ | Should -Be "zzz"
        }
        finally {
            $session | Remove-ProcessExSession
        }
    }

    It "Gets environment and command line x86 -> x64" -Skip:(-not [Environment]::Is64BitOperatingSystem) {
        $cmdLine = "pwsh.exe C:\Program Files\file\\"" KEY=""value test"" {""json"": ""value""}"

        $session = New-ProcessExSession -FilePath $pwsh32
        try {
            $actual = Invoke-Command $session {
                $procParams = @{
                    CommandLine = $args[0]
                    ApplicationName = "C:\Windows\Sysnative\WindowsPowerShell\v1.0\powershell.exe"
                    CreationFlags = [ProcessEx.CreationFlags]::Suspended
                    Environment = @{
                        AAA = "aaa"
                        ZZZ = "zzz"
                    }
                    PassThru = $true
                }

                $proc = Start-ProcessEx @procParams
                try {
                    Get-ProcessEx -Process $proc
                }
                finally {
                    $proc | Stop-Process -Force
                }

            } -ArgumentList $cmdLine, $pwsh64

            $actual.Executable | Should -Be $pwshNative
            $actual.CommandLine | Should -Be $cmdLine
            $actual.Environment.Count | Should -Be 2
            $actual.Environment.AAA | Should -Be "aaa"
            $actual.Environment.ZZZ | Should -Be "zzz"
        }
        finally {
            $session | Remove-ProcessExSession
        }
    }
}

Describe "Start-ProcessEx" {
    It "Starts a new process without passthru" {
        $actual = Start-ProcessEx cmd.exe /c echo hi
        $actual | Should -Be $null
    }

    It "Starts a new process with passthru" {
        $actual = Start-ProcessEx powershell.exe sleep 1 -PassThru
        $actual -is ([ProcessEx.ProcessInfo]) | Should -Be $true
        $actual.ExitCode | Should -Be $null
        $actual | Wait-Process
        $actual.ExitCode | Should -Be 0
    }

    It "Waits until process is finished" {
        $actual = Start-ProcessEx powershell.exe sleep 1 -PassThru -Wait
        $actual -is ([ProcessEx.ProcessInfo]) | Should -Be $true
        $actual.ExitCode | Should -Be 0
    }

    It "Waits until all child processes are finished" {
        $stdout = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $stdin = [System.IO.Pipes.AnonymousPipeServerStream]::new("Out", "Inheritable")
        try {
            $si = New-StartupInfo -StandardOutput $stdout.ClientSafePipeHandle -StandardInput $stdin.ClientSafePipeHandle
            $procParams = @{
                FilePath = 'powershell.exe'
                ArgumentList = '-Command', "-"
                StartupInfo = $si
                Wait = $true
                PassThru = $true
            }

            $projectPath = Split-Path -Path $PSScriptRoot -Parent
            $ps = [PowerShell]::Create()
            [void]$ps.AddCommand("Import-Module").AddParameter("Name", "$projectPath\output\ProcessEx").AddStatement()
            $task = $ps.AddCommand('Start-ProcessEx').AddParameters($procParams).BeginInvoke()
            try {
                $stdinWriter = [IO.StreamWriter]::new($stdin)
                $stdinWriter.AutoFlush = $true
                $stdoutReader = [IO.StreamReader]::new($stdout)

                $stdinWriter.WriteLine('$pid')
                [int]$childPid1 = $stdoutReader.ReadLine()

                # Wait until we know the process has started (read the input) before disposing the client copies
                $stdin.DisposeLocalCopyOfClientHandle()
                $stdout.DisposeLocalCopyOfClientHandle()

                $stdinWriter.WriteLine('(Start-Process powershell.exe -PassThru).Id')
                [int]$childPid2 = $stdoutReader.ReadLine()
                $stdinWriter.WriteLine('(Start-Process powershell.exe -PassThru).Id')
                [int]$childPid3 = $stdoutReader.ReadLine()
                $stdoutReader.Dispose()

                try {
                    $spawnedProcess = Get-Process -Id $childPid1 -ErrorAction SilentlyContinue
                    $spawnedProcess | Should -Not -Be $null
                    $task.IsCompleted | Should -Be $false

                    $stdinWriter.WriteLine('exit')
                    $stdinWriter.Dispose()
                    $spawnedProcess.WaitForExit(5000)
                    $task.IsCompleted | Should -Be $false

                    Stop-Process -Id $childPid2 -Force
                    Wait-Process -Id $childPid2 -Timeout 5 -ErrorAction SilentlyContinue
                    $task.isCompleted | Should -Be $false

                    Stop-Process -Id $childPid3 -Force
                    Wait-Process -Id $childPid3 -Timeout 5 -ErrorAction SilentlyContinue
                    $task.AsyncWaitHandle.WaitOne(5000)
                    $task.isCompleted | Should -Be $true
                }
                finally {
                    Stop-Process -Id $childPid1 -Force -ErrorAction SilentlyContinue
                    Stop-Process -Id $childPid2 -Force -ErrorAction SilentlyContinue
                    Stop-Process -Id $childPid3 -Force -ErrorAction SilentlyContinue
                }
            }
            finally {
                $ps.EndInvoke($task)
            }
        }
        finally {
            $stdout.Dispose()
            $stdin.Dispose()
        }
    }

    It "Inherits current working directory" {
        Push-Location $TestDrive
        try {
            $path = (Get-Item -LiteralPath $TestDrive).FullName
            $session = New-ProcessExSession
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

    It "Sets working directory" {
        $path = (Get-Item -LiteralPath $TestDrive).FullName
        $session = New-ProcessExSession -WorkingDirectory $path
        try {
            $actual = Invoke-Command $session { [System.Environment]::CurrentDirectory }
        }
        finally {
            $session | Remove-ProcessExSession
        }

        $actual | Should -Be $path
    }

    It "Creates suspended process" {
        $proc = Start-ProcessEx cmd.exe /c exit 0 -CreationFlags Suspended, NewConsole -PassThru
        $actual = Get-Process -Id $proc.Id -ErrorAction Stop
        $null -eq $actual | Should -Be $false
        [ProcessEx.ProcessRunner]::ResumeThread($proc.Thread)
        $proc | Wait-Process
        $proc.ExitCode | Should -Be 0
    }

    It "Escapes arguments" {
        $arguments = @(
            'C:\Program Files\file\'
            'arg with " quote'
            '{ "json": "value\"" }'
            'normal'
            ''
        )
        $pwshPath = (Get-Command -Name powershell.exe -CommandType Application).Path
        $expected = $pwshPath + ' "C:\Program Files\file\\" "arg with \" quote" "{ \"json\": \"value\\\"\" }" normal ""'

        $proc = Start-ProcessEx -FilePath powershell.exe -ArgumentList $arguments -PassThru -CreationFlags Suspended
        try {
            $proc.CommandLine | Should -Be $expected
        }
        finally {
            $proc | Stop-Process -Force
        }
    }

    It "Uses literal arguments" {
        $pwshPath = (Get-Command -Name powershell.exe -CommandType Application).Path
        $cmdLine = "powershell.exe C:\Program Files\file\\"" KEY=""value test"" {""json"": ""value""}"
        $proc = Start-ProcessEx -CommandLine $cmdLine -PassThru -CreationFlags Suspended
        try {
            if ([Environment]::Is64BitProcess) {
                $proc.Executable | Should -Be  $pwshPath
            }
            else {
                $proc.Executable | Should -Be "C:\Windows\SysWow64\WindowsPowerShell\v1.0\powershell.exe"
            }
            $proc.CommandLine | Should -Be $cmdLine
        }
        finally {
            $proc | Stop-Process -Force
        }
    }

    It "Uses command line with custom executable" {
        $pwshPath = (Get-Command -Name powershell.exe -CommandType Application).Path
        $cmdLine = "pwsh.exe C:\Program Files\file\\"" KEY=""value test"" {""json"": ""value""}"
        $proc = Start-ProcessEx -CommandLine $cmdLine -Applicationname $pwshPath -PassThru -CreationFlags Suspended
        try {
            if ([Environment]::Is64BitProcess) {
                $proc.Executable | Should -Be  $pwshPath
            }
            else {
                $proc.Executable | Should -Be "C:\Windows\SysWow64\WindowsPowerShell\v1.0\powershell.exe"
            }
            $proc.CommandLine | Should -Be $cmdLine
        }
        finally {
            $proc | Stop-Process -Force
        }
    }

    It "Fails to create process with bad path" {
        $err = $null
        Start-ProcessEx bad.exe -ErrorAction SilentlyContinue -ErrorVariable err
        $err.Count | Should -Be 1
        [string]$err[0] | Should -BeLike 'Failed to create process *'
    }

    It "Inherits current environment" {
        $env:Testing = "abc"
        $env = [System.Environment]::GetEnvironmentVariables()

        $session = New-ProcessExSession
        try {
            $actual = Invoke-Command $session { [System.Environment]::GetEnvironmentVariables() }
        }
        finally {
            $session | Remove-ProcessExSession
        }

        $actual.Count | Should -Be $env.Count
        $actual.Testing | Should -Be "abc"
    }

    It "Uses custom environment" {
        $env = [System.Environment]::GetEnvironmentVariables()
        $env.Testing = "abc"
        $session = New-ProcessExSession -Environment $env
        try {
            $actual = Invoke-Command $session { [System.Environment]::GetEnvironmentVariables() }
        }
        finally {
            $session | Remove-ProcessExSession
        }

        $actual.Count | Should -Be $env.Count
        $actual.Testing | Should -Be "abc"
    }

    It "Uses new environment" {
        $env:Testing = "abc"
        $env = [System.Environment]::GetEnvironmentVariables()
        $expected = Get-TokenEnvironment

        $session = New-ProcessExSession -UseNewEnvironment
        try {
            $actual = Invoke-Command $session { [System.Environment]::GetEnvironmentVariables() }
        }
        finally {
            $session | Remove-ProcessExSession
        }

        if ([System.Environment]::Is64BitProcess) {
            $actual.Count | Should -Not -Be $env.Count
            $actual.Count | Should -Be $expected.Count
        }
        else {
            # Windows automatically applies an extra var for syswow64
            $actual.Count | Should -Not -Be ($env.Count + 1)
            $actual.Count | Should -Be ($expected.Count + 1)
        }

        $actual.Contains("Testing") | Should -Be $false
    }

    It "Redirects stdio" {
        $stdout = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $stderr = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $stdin = [System.IO.Pipes.AnonymousPipeServerStream]::new("Out", "Inheritable")
        try {
            $si = New-StartupInfo -StandardOutput $stdout.ClientSafePipeHandle -StandardError $stderr.ClientSafePipeHandle -StandardInput $stdin.ClientSafePipeHandle
            $procParams = @{
                FilePath = 'powershell.exe'
                ArgumentList = '-Command', '-'
                StartupInfo = $si
            }
            Start-ProcessEx @procParams
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

    It "Uses ConPTY IO" {
        $outputPipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $inputPipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("Out", "Inheritable")

        $pty = New-ConPTY -Width 60 -Height 80 -InputPipe $inputPipe.ClientSafePipeHandle -OutputPipe $outputPipe.ClientSafePipeHandle
        try {
            $inputPipe.ClientSafePipeHandle.Dispose()
            $outputPipe.ClientSafePipeHandle.Dispose()
            $si = New-StartupInfo -ConPTY $pty
            $proc = Start-ProcessEx powershell -StartupInfo $si -PassThru

            $inputWriter = [IO.StreamWriter]::new($inputPipe)
            $inputWriter.WriteLine("echo 'hi'")
            $inputWriter.WriteLine("exit")
            $inputWriter.Dispose()

            $outputReader = [IO.StreamReader]::new($outputPipe)
            $proc | Wait-Process -ErrorAction SilentlyContinue
            $pty.Dispose()
            $actual = $outputReader.ReadToEnd()
            $outputReader.Dispose()

            $actual | Should -Not -Be ""
        }
        finally {
            $pty.Dispose()
            $outputPipe.Dispose()
            $inputPipe.Dispose()
        }
    }

    It "Inherits all handles" {
        $pipe1 = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $pipe2 = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None")
        $session = New-ProcessExSession
        try {
            $actual = Invoke-Command -Session $session {
                $ErrorActionPreference = 'Stop'

                $pipe1 = [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[0])
                $sw = [System.IO.StreamWriter]::new($pipe1)
                $sw.WriteLine("message")
                $sw.Dispose()

                # Won't work as it wasn't set as inheritable.
                $failed = $false
                try {
                    [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[2])
                }
                catch {
                    $failed = $true
                }
                $failed

            } -ArgumentList @($pipe1.GetClientHandleAsString(), $pipe2.GetClientHandleAsString())
            $pipe1.DisposeLocalCopyOfClientHandle()
            $pipe2.DisposeLocalCopyOfClientHandle()

            $actual | Should -Be $true

            $reader = [IO.StreamReader]::new($pipe1)
            $reader.ReadToEnd().Trim() | Should -Be "message"
            $reader.Dispose()
        }
        finally {
            $session | Remove-ProcessExSession
            $pipe1.Dispose()
            $pipe2.Dispose()
        }
    }

    It "Disables handle inheritance" {
        $pipe1 = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $pipe2 = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None")
        $session = New-ProcessExSession -DisableInheritance
        try {
            $actual = Invoke-Command -Session $session {
                $ErrorActionPreference = 'Stop'

                # Won't work as the process was created without any inheritance allowed
                $failed = $false
                try {
                    [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[0])
                }
                catch {
                    $failed = $true
                }
                $failed

                $failed = $false
                try {
                    [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[1])
                }
                catch {
                    $failed = $true
                }
                $failed

            } -ArgumentList @($pipe1.GetClientHandleAsString(), $pipe2.GetClientHandleAsString())
            $pipe1.DisposeLocalCopyOfClientHandle()
            $pipe2.DisposeLocalCopyOfClientHandle()

            $actual.Count | Should -Be 2
            $actual[0] | Should -Be $true
            $actual[1] | Should -Be $true
        }
        finally {
            $session | Remove-ProcessExSession
            $pipe1.Dispose()
            $pipe2.Dispose()
        }
    }

    It "Inherits explicit handles" {
        $pipe1 = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $pipe2 = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None")
        $pipe3 = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $si = New-StartupInfo -InheritedHandle $pipe1.ClientSafePipeHandle, $pipe2.ClientSafePipeHandle
        $session = New-ProcessExSession -StartupInfo $si
        try {
            $actual = Invoke-Command -Session $session {
                $ErrorActionPreference = 'Stop'

                $pipe1 = [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[0])
                $sw = [System.IO.StreamWriter]::new($pipe1)
                $sw.WriteLine("message 1")
                $sw.Dispose()

                $pipe2 = [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[1])
                $sw = [System.IO.StreamWriter]::new($pipe2)
                $sw.WriteLine("message 2")
                $sw.Dispose()

                # Won't work as it wasn't in the explicit handle list.
                $failed = $false
                try {
                    [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[2])
                }
                catch {
                    $failed = $true
                }
                $failed

            } -ArgumentList @($pipe1.GetClientHandleAsString(), $pipe2.GetClientHandleAsString(), $pipe3.GetClientHandleAsString())
            $pipe1.DisposeLocalCopyOfClientHandle()
            $pipe2.DisposeLocalCopyOfClientHandle()
            $pipe3.DisposeLocalCopyOfClientHandle()

            $actual | Should -Be $true

            $reader = [IO.StreamReader]::new($pipe1)
            $reader.ReadToEnd().Trim() | Should -Be "message 1"
            $reader.Dispose()

            $reader = [IO.StreamReader]::new($pipe2)
            $reader.ReadToEnd().Trim() | Should -Be "message 2"
            $reader.Dispose()
        }
        finally {
            $session | Remove-ProcessExSession
            $pipe1.Dispose()
            $pipe2.Dispose()
            $pipe3.Dispose()
        }
    }

    It "Parent process" {
        $parentProc = Start-ProcessEx powershell.exe -PassThru
        try {
            $si = New-StartupInfo -ParentProcess $parentProc
            $childProc = Start-ProcessEx powershell.exe -PassThru -StartupInfo $si

            try {
                $actual = Get-ProcessEx -Process $childProc
                $actual.ParentProcessId | Should -Be $parentProc.ProcessId
            }
            finally {
                $childProc | Stop-Process -Force
            }
        }
        finally {
            $parentProc | Stop-Process -Force
        }
    }

    It "Parent process with redirected stdio" {
        $parentProc = Start-ProcessEx powershell.exe -PassThru
        $stdout = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $stderr = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None") # Tests that it is explicitly set to inheritable
        $stdin = [System.IO.Pipes.AnonymousPipeServerStream]::new("Out", "Inheritable")
        try {
            $siParams = @{
                StandardOutput = $stdout.ClientSafePipeHandle
                StandardError = $stderr.ClientSafePipeHandle
                StandardInput = $stdin.ClientSafePipeHandle
                ParentProcess = $parentProc
            }
            $si = New-StartupInfo @siParams
            $procParams = @{
                FilePath = 'powershell.exe'
                ArgumentList = '-Command', '-'
                StartupInfo = $si
                PassThru = $true
            }
            $proc = Start-ProcessEx @procParams
            try {
                $stdout.DisposeLocalCopyOfClientHandle()
                $stderr.DisposeLocalCopyOfClientHandle()
                $stdin.DisposeLocalCopyOfClientHandle()

                $proc.ParentProcessId | Should -Be $parentProc.ProcessId

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
                $proc | Stop-Process -Force -ErrorAction SilentlyContinue
            }
        }
        finally {
            $parentProc | Stop-Process -Force
            $stdout.Dispose()
            $stderr.Dispose()
            $stdin.Dispose()
        }
    }

    It "Parent process with inherited handles" {
        $parentProc = Start-ProcessEx powershell.exe -PassThru
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None")
        try {
            $dupPipe = Copy-HandleToProcess -Handle $pipe.ClientSafePipeHandle -Process $parentProc -Inherit -OwnHandle
            $pipe.DisposeLocalCopyOfClientHandle()

            $siParams = @{
                InheritedHandle = $dupPipe
                ParentProcess = $parentProc
            }
            $si = New-StartupInfo @siParams
            $session = New-ProcessExSession -StartupInfo $si
            try {
                $dupPipe.Dispose()

                Invoke-Command -Session $session {
                    [CmdletBinding()]
                    param ([string]$Handle)

                    $pipe = [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $Handle)
                    $sw = [System.IO.StreamWriter]::new($pipe)
                    $sw.WriteLine("message")
                    $sw.Dispose()
                } -ArgumentList ([string]$dupPipe.DangerousGetHandle())

                $reader = [IO.StreamReader]::new($pipe)
                $actual = $reader.ReadToEnd()
                $reader.Dispose()

                $actual.Trim() | Should -Be "message"
            }
            finally {
                $session | Remove-ProcessExSession
            }
        }
        finally {
            $parentProc | Stop-Process -Force
            $pipe.Dispose()
        }
    }

    It "Sets a custom desktop" {
        $desktopName = "ProcessExDesktop"
        $desktop = [ProcessExTests.Native]::CreateDesktopEx($desktopName, "NONE",
            [ProcessEx.Security.DesktopAccessRights]::AllAccess, 128)
        try {
            $si = New-StartupInfo -Desktop $desktopName
            $session = New-ProcessExSession -StartupInfo $si
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

    It "Sets explicit desktop same as caller" {
        $desktop = [ProcessExTests.Native]::GetThreadDesktop([ProcessExTests.Native]::GetCurrentThreadId())
        $desktopName = [ProcessExTests.Native]::GetUserObjectName($desktop)

        $si = New-StartupInfo -Desktop $desktopName
        $session = New-ProcessExSession -StartupInfo $si
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


    It "Set explicit station\desktop with station same as caller" {
        $station = [ProcessExTests.Native]::GetProcessWindowStation()
        $stationName = [ProcessExTests.Native]::GetUserObjectName($station)

        $desktopName = "ProcessExDesktop"
        $desktop = [ProcessExTests.Native]::CreateDesktopEx($desktopName, "NONE",
            [ProcessEx.Security.DesktopAccessRights]::AllAccess, 128)
        try {
            $si = New-StartupInfo -Desktop "$stationName\$desktopName"
            $session = New-ProcessExSession -StartupInfo $si
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

    It "Sets a custom station and desktop" {
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
            $session = New-ProcessExSession -StartupInfo $si
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

    It "Sets explicit station and desktop same as caller" {
        $station = [ProcessExTests.Native]::GetProcessWindowStation()
        $stationName = [ProcessExTests.Native]::GetUserObjectName($station)

        $desktop = [ProcessExTests.Native]::GetThreadDesktop([ProcessExTests.Native]::GetCurrentThreadId())
        $desktopName = [ProcessExTests.Native]::GetUserObjectName($desktop)

        $si = New-StartupInfo -Desktop "$stationName\$desktopName"
        $session = New-ProcessExSession -StartupInfo $si
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

    It "Uses process security attributes" {
        # FUTURE: Make this into a cmdlet so users can easily make this
        $procSec = [ProcessEx.Security.ProcessSecurity]::new(
            (Get-ProcessEx -Id $pid).Process,
            [System.Security.AccessControl.AccessControlSections]"Access, Group, Owner")
        $procSec.AddAccessRule($procSec.AccessRuleFactory(
            [System.Security.Principal.SecurityIdentifier]::new("S-1-1-0"), # Everybody
            [ProcessEx.Security.ProcessAccessRights]::AllAccess,
            $false,
            [System.Security.AccessControl.InheritanceFlags]::None,
            [System.Security.AccessControl.PropagationFlags]::None,
            [System.Security.AccessControl.AccessControlType]::Allow))
        $procSDDL = $procSec.GetSecurityDescriptorSddlForm("Access, Group, Owner")

        $procAttributes = [ProcessEx.Security.SecurityAttributes]@{
            InheritHandle = $true
            SecurityDescriptor = $procSec
        }
        $threadAttributes = [ProcessEx.Security.SecurityAttributes]@{
            InheritHandle = $false
        }
        $projectPath = Split-Path -Path $PSScriptRoot -Parent

        $proc = Start-ProcessEx powershell.exe -ProcessAttribute $procAttributes -ThreadAttribute $threadAttributes -PassThru
        $session = New-ProcessExSession -WorkingDirectory $projectPath
        try {
            $actualSec = [ProcessEx.Security.ProcessSecurity]::new(
                $proc.Process,
                [System.Security.AccessControl.AccessControlSections]"Access, Group, Owner")
            $actualSec.GetSecurityDescriptorSddlForm("Access, Group, Owner") | Should -Be $procSDDL

            $actual = Invoke-Command -Session $session {
                $ErrorActionPreference = 'Stop'

                $procHandle = [IntPtr][Int64]$args[0]
                $procSafeHandle = [Microsoft.Win32.SafeHandles.SafeProcessHandle]::new($procHandle, $true)
                try {
                    [ProcessExTests.Native]::GetProcessId($procSafeHandle)

                    $procSec = [ProcessEx.Security.ProcessSecurity]::new(
                        $procSafeHandle,
                        [System.Security.AccessControl.AccessControlSections]"Access, Group, Owner")
                    $procSec.GetSecurityDescriptorSddlForm("Access, Group, Owner")
                }
                finally {
                    $procSafeHandle.Dispose()
                }

                # Wasn't marked as inheritable so this is expected to fail
                $threadHandle = [IntPtr][Int64]$args[1]
                $threadSafeHandle = [Microsoft.Win32.SafeHandles.SafeProcessHandle]::new($threadHandle, $true)

                try {
                    [void][ProcessExTests.Native]::GetThreadId($threadSafeHandle)
                }
                catch [System.ComponentModel.Win32Exception] {
                    $_.Exception.NativeErrorCode
                }

            } -ArgumentList @([string]$proc.Process.DangerousGetHandle(), [string]$proc.Thread.DangerousGetHandle())

            $actual.Count | Should -Be 3
            $actual[0] | Should -Be $proc.ProcessId
            $actual[1] | Should -Be $procSDDL
            $actual[2] | Should -Be 6 # ERROR_INVALID_HANDLE
        }
        finally {
            $session | Remove-ProcessExSession
            $proc | Stop-Process -Force
        }
    }

    It "Uses thread security attributes" {
        # FUTURE: Make this into a cmdlet so users can easily make this
        $threadSec = [ProcessEx.Security.ThreadSecurity]::new(
            [ProcessExTests.Native]::GetCurrentThread(),
            [System.Security.AccessControl.AccessControlSections]"Access, Group, Owner")
        $threadSec.AddAccessRule($threadSec.AccessRuleFactory(
            [System.Security.Principal.SecurityIdentifier]::new("S-1-1-0"), # Everybody
            [ProcessEx.Security.ThreadAccessRights]::AllAccess,
            $false,
            [System.Security.AccessControl.InheritanceFlags]::None,
            [System.Security.AccessControl.PropagationFlags]::None,
            [System.Security.AccessControl.AccessControlType]::Allow))
        $threadSDDL = $threadSec.GetSecurityDescriptorSddlForm("Access, Group, Owner")

        $procAttributes = [ProcessEx.Security.SecurityAttributes]@{
            InheritHandle = $false
        }
        $threadAttributes = [ProcessEx.Security.SecurityAttributes]@{
            InheritHandle = $true
            SecurityDescriptor = $threadSec
        }
        $projectPath = Split-Path -Path $PSScriptRoot -Parent

        $proc = Start-ProcessEx powershell.exe -ProcessAttribute $procAttributes -ThreadAttribute $threadAttributes -PassThru
        $session = New-ProcessExSession -WorkingDirectory $projectPath
        try {
            $actualSec = [ProcessEx.Security.ThreadSecurity]::new(
                $proc.Thread,
                [System.Security.AccessControl.AccessControlSections]"Access, Group, Owner")
            $actualSec.GetSecurityDescriptorSddlForm("Access, Group, Owner") | Should -Be $threadSDDL

            $actual = Invoke-Command -Session $session {
                $ErrorActionPreference = 'Stop'

                # Wasn't marked as inheritable so this is expected to fail
                $threadHandle = [IntPtr][Int64]$args[1]
                $threadSafeHandle = [Microsoft.Win32.SafeHandles.SafeProcessHandle]::new($threadHandle, $true)

                try {
                    [ProcessExTests.Native]::GetThreadId($threadSafeHandle)

                    $threadSec = [ProcessEx.Security.ThreadSecurity]::new(
                        $threadSafeHandle,
                        [System.Security.AccessControl.AccessControlSections]"Access, Group, Owner")
                    $threadSec.GetSecurityDescriptorSddlForm("Access, Group, Owner")
                }
                finally {
                    $threadSafeHandle.Dispose()
                }

                $procHandle = [IntPtr][Int64]$args[0]
                $procSafeHandle = [Microsoft.Win32.SafeHandles.SafeProcessHandle]::new($procHandle, $true)

                try {
                    [void][ProcessExTests.Native]::GetProcessId($procSafeHandle)
                }
                catch [System.ComponentModel.Win32Exception] {
                    $_.Exception.NativeErrorCode
                }

            } -ArgumentList @([string]$proc.Process.DangerousGetHandle(), [string]$proc.Thread.DangerousGetHandle())

            $actual.Count | Should -Be 3
            $actual[0] | Should -Be $proc.ThreadId
            $actual[1] | Should -Be $threadSDDL
            $actual[2] | Should -Be 6 # ERROR_INVALID_HANDLE
        }
        finally {
            $session | Remove-ProcessExSession
            $proc | Stop-Process -Force
        }
    }

    It "Fails with suspend and -Wait" {
        $err = $null
        Start-ProcessEx pwsh -CreationFlags Suspended -Wait -ErrorAction SilentlyContinue -ErrorVariable err
        $err.Count | Should -Be 1
        [string]$err[0] | Should -Be "Cannot use -Wait with -CreationFlags Suspended"

    }

    It "Fails with inherited handles and -DisableInheritance" {
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        try {
            $err = $null
            $si = New-StartupInfo -InheritedHandle $pipe.ClientSafePipeHandle
            Start-ProcessEx pwsh -StartupInfo $si -DisableInheritance -ErrorAction SilentlyContinue -ErrorVariable err
            $err.Count | Should -Be 1
            [string]$err[0] | Should -Be "Cannot -DisableInheritance with explicit inherited handles in StartupInfo"
        }
        finally {
            $pipe.Dispose()
        }
    }

    It "Failed with explicit environment and -UseNewEnvironment" {
        $err = $null
        Start-ProcessEx pwsh -Environment @{ foo = 'bar' } -UseNewEnvironment -ErrorAction SilentlyContinue -ErrorVariable err
        $err.Count | Should -Be 1
        [string]$err[0] | Should -Be "Cannot use -UseNewEnvironment with environment vars"
    }
}

# Start-ProcessEx with -Token calls CreateProcessAsUser which requires this privilege
Describe "Start-ProcessEx with Token" -Skip:(Get-ProcessPrivilege -Name SeAssignPrimaryTokenPrivilege).IsRemoved {
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

    It "Runs with token" {
        $session = New-ProcessExSession -Token $token
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

            $session = New-ProcessExSession -Token $tokenToUse
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

            $session = New-ProcessExSession -Token $tokenToUse
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
        $sessionid = Get-TokenSessionid -Token (Get-ProcessToken -Process $pid -Access Query)

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
                Set-TokenSessionId -Token $systemToken -SessionId $sessionId
            }
            finally {
                [ProcessExTests.Native]::RevertToSelf()
            }

            $session = $null
            try {
                $session = New-ProcessExSession -Token $systemToken
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

    It "Inherits current working directory" {
        Push-Location $TestDrive
        try {
            $path = (Get-Item -LiteralPath $TestDrive).FullName
            $session = New-ProcessExSession -Token $token
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

    It "Sets working directory" {
        $path = (Get-Item -LiteralPath $TestDrive).FullName
        $session = New-ProcessExSession -WorkingDirectory $path -Token $token
        try {
            $actual = Invoke-Command $session { [System.Environment]::CurrentDirectory }
        }
        finally {
            $session | Remove-ProcessExSession
        }

        $actual | Should -Be $path
    }

    It "Inherits current environment" {
        $env:Testing = "abc"
        $env = [System.Environment]::GetEnvironmentVariables()

        $session = New-ProcessExSession -Token $token
        try {
            $actual = Invoke-Command $session { [System.Environment]::GetEnvironmentVariables() }
        }
        finally {
            $session | Remove-ProcessExSession
        }

        $actual.Count | Should -Be $env.Count
        $actual.Testing | Should -Be "abc"
    }

    It "Uses custom environment" {
        $env = [System.Environment]::GetEnvironmentVariables()
        $env.Testing = "abc"
        $session = New-ProcessExSession -Environment $env -Token $token
        try {
            $actual = Invoke-Command $session { [System.Environment]::GetEnvironmentVariables() }
        }
        finally {
            $session | Remove-ProcessExSession
        }

        $actual.Count | Should -Be $env.Count
        $actual.Testing | Should -Be "abc"
    }

    It "Uses new environment" {
        $env:Testing = "abc"
        $env = [System.Environment]::GetEnvironmentVariables()
        $expected = Get-TokenEnvironment -Token $token

        $session = New-ProcessExSession -UseNewEnvironment -Token $token
        try {
            $actual = Invoke-Command $session { [System.Environment]::GetEnvironmentVariables() }
        }
        finally {
            $session | Remove-ProcessExSession
        }

        if ([System.Environment]::Is64BitProcess) {
            $actual.Count | Should -Not -Be $env.Count
            $actual.Count | Should -Be $expected.Count
        }
        else {
            # Windows automatically applies an extra var for syswow64
            $actual.Count | Should -Not -Be ($env.Count + 1)
            $actual.Count | Should -Be ($expected.Count + 1)
        }
        $actual.Contains("Testing") | Should -Be $false
    }

    It "Redirects stdio" {
        $stdout = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $stderr = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $stdin = [System.IO.Pipes.AnonymousPipeServerStream]::new("Out", "Inheritable")
        try {
            $si = New-StartupInfo -StandardOutput $stdout.ClientSafePipeHandle -StandardError $stderr.ClientSafePipeHandle -StandardInput $stdin.ClientSafePipeHandle
            $procParams = @{
                FilePath = 'powershell.exe'
                ArgumentList = '-Command', '-'
                StartupInfo = $si
                Token = $token
            }
            Start-ProcessEx @procParams
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

    It "Uses ConPTY IO" {
        $outputPipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $inputPipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("Out", "Inheritable")

        $pty = New-ConPTY -Width 60 -Height 80 -InputPipe $inputPipe.ClientSafePipeHandle -OutputPipe $outputPipe.ClientSafePipeHandle
        try {
            $inputPipe.ClientSafePipeHandle.Dispose()
            $outputPipe.ClientSafePipeHandle.Dispose()
            $si = New-StartupInfo -ConPTY $pty
            $proc = Start-ProcessEx powershell -StartupInfo $si -PassThru -Token $token

            $inputWriter = [IO.StreamWriter]::new($inputPipe)
            $inputWriter.WriteLine("echo 'hi'")
            $inputWriter.WriteLine("exit")
            $inputWriter.Dispose()

            $outputReader = [IO.StreamReader]::new($outputPipe)
            $proc | Wait-Process -ErrorAction SilentlyContinue
            $pty.Dispose()
            $actual = $outputReader.ReadToEnd()
            $outputReader.Dispose()

            $actual | Should -Not -Be ""
        }
        finally {
            $pty.Dispose()
            $outputPipe.Dispose()
            $inputPipe.Dispose()
        }
    }

    It "Inherits all handles" {
        $pipe1 = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $pipe2 = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None")
        $session = New-ProcessExSession -Token $token
        try {
            $actual = Invoke-Command -Session $session {
                $ErrorActionPreference = 'Stop'

                $pipe1 = [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[0])
                $sw = [System.IO.StreamWriter]::new($pipe1)
                $sw.WriteLine("message")
                $sw.Dispose()

                # Won't work as it wasn't set as inheritable.
                $failed = $false
                try {
                    [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[2])
                }
                catch {
                    $failed = $true
                }
                $failed

            } -ArgumentList @($pipe1.GetClientHandleAsString(), $pipe2.GetClientHandleAsString())
            $pipe1.DisposeLocalCopyOfClientHandle()
            $pipe2.DisposeLocalCopyOfClientHandle()

            $actual | Should -Be $true

            $reader = [IO.StreamReader]::new($pipe1)
            $reader.ReadToEnd().Trim() | Should -Be "message"
            $reader.Dispose()
        }
        finally {
            $session | Remove-ProcessExSession
            $pipe1.Dispose()
            $pipe2.Dispose()
        }
    }

    It "Disables handle inheritance" {
        $pipe1 = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $pipe2 = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None")
        $session = New-ProcessExSession -DisableInheritance -Token $token
        try {
            $actual = Invoke-Command -Session $session {
                $ErrorActionPreference = 'Stop'

                # Won't work as the process was created without any inheritance allowed
                $failed = $false
                try {
                    [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[0])
                }
                catch {
                    $failed = $true
                }
                $failed

                $failed = $false
                try {
                    [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[1])
                }
                catch {
                    $failed = $true
                }
                $failed

            } -ArgumentList @($pipe1.GetClientHandleAsString(), $pipe2.GetClientHandleAsString())
            $pipe1.DisposeLocalCopyOfClientHandle()
            $pipe2.DisposeLocalCopyOfClientHandle()

            $actual.Count | Should -Be 2
            $actual[0] | Should -Be $true
            $actual[1] | Should -Be $true
        }
        finally {
            $session | Remove-ProcessExSession
            $pipe1.Dispose()
            $pipe2.Dispose()
        }
    }

    It "Inherits explicit handles" {
        $pipe1 = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $pipe2 = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None")
        $pipe3 = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $si = New-StartupInfo -InheritedHandle $pipe1.ClientSafePipeHandle, $pipe2.ClientSafePipeHandle
        $session = New-ProcessExSession -StartupInfo $si -Token $token
        try {
            $actual = Invoke-Command -Session $session {
                $ErrorActionPreference = 'Stop'

                $pipe1 = [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[0])
                $sw = [System.IO.StreamWriter]::new($pipe1)
                $sw.WriteLine("message 1")
                $sw.Dispose()

                $pipe2 = [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[1])
                $sw = [System.IO.StreamWriter]::new($pipe2)
                $sw.WriteLine("message 2")
                $sw.Dispose()

                # Won't work as it wasn't in the explicit handle list.
                $failed = $false
                try {
                    [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[2])
                }
                catch {
                    $failed = $true
                }
                $failed

            } -ArgumentList @($pipe1.GetClientHandleAsString(), $pipe2.GetClientHandleAsString(), $pipe3.GetClientHandleAsString())
            $pipe1.DisposeLocalCopyOfClientHandle()
            $pipe2.DisposeLocalCopyOfClientHandle()
            $pipe3.DisposeLocalCopyOfClientHandle()

            $actual | Should -Be $true

            $reader = [IO.StreamReader]::new($pipe1)
            $reader.ReadToEnd().Trim() | Should -Be "message 1"
            $reader.Dispose()

            $reader = [IO.StreamReader]::new($pipe2)
            $reader.ReadToEnd().Trim() | Should -Be "message 2"
            $reader.Dispose()
        }
        finally {
            $session | Remove-ProcessExSession
            $pipe1.Dispose()
            $pipe2.Dispose()
            $pipe3.Dispose()
        }
    }

    It "Parent process" {
        $parentProc = Start-ProcessEx powershell.exe -PassThru
        try {
            $si = New-StartupInfo -ParentProcess $parentProc
            $childProc = Start-ProcessEx powershell.exe -PassThru -StartupInfo $si -Token $token

            try {
                $actual = Get-ProcessEx -Process $childProc
                $actual.ParentProcessId | Should -Be $parentProc.ProcessId
            }
            finally {
                $childProc | Stop-Process -Force
            }
        }
        finally {
            $parentProc | Stop-Process -Force
        }
    }

    It "Parent process with redirected stdio" {
        $parentProc = Start-ProcessEx powershell.exe -PassThru
        $stdout = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $stderr = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None") # Tests that it is explicitly set to inheritable
        $stdin = [System.IO.Pipes.AnonymousPipeServerStream]::new("Out", "Inheritable")
        try {
            $siParams = @{
                StandardOutput = $stdout.ClientSafePipeHandle
                StandardError = $stderr.ClientSafePipeHandle
                StandardInput = $stdin.ClientSafePipeHandle
                ParentProcess = $parentProc
            }
            $si = New-StartupInfo @siParams
            $procParams = @{
                FilePath = 'powershell.exe'
                ArgumentList = '-Command', '-'
                StartupInfo = $si
                PassThru = $true
            }
            $proc = Start-ProcessEx @procParams -Token $token
            try {
                $stdout.DisposeLocalCopyOfClientHandle()
                $stderr.DisposeLocalCopyOfClientHandle()
                $stdin.DisposeLocalCopyOfClientHandle()

                $proc.ParentProcessId | Should -Be $parentProc.ProcessId

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
                $proc | Stop-Process -Force -ErrorAction SilentlyContinue
            }
        }
        finally {
            $parentProc | Stop-Process -Force
            $stdout.Dispose()
            $stderr.Dispose()
            $stdin.Dispose()
        }
    }

    It "Parent process with inherited handles" {
        $parentProc = Start-ProcessEx powershell.exe -PassThru
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None")
        try {
            $dupPipe = Copy-HandleToProcess -Handle $pipe.ClientSafePipeHandle -Process $parentProc -Inherit -OwnHandle
            $pipe.DisposeLocalCopyOfClientHandle()

            $siParams = @{
                InheritedHandle = $dupPipe
                ParentProcess = $parentProc
            }
            $si = New-StartupInfo @siParams
            $session = New-ProcessExSession -StartupInfo $si -Token $token
            try {
                $dupPipe.Dispose()

                Invoke-Command -Session $session {
                    [CmdletBinding()]
                    param ([string]$Handle)

                    $pipe = [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $Handle)
                    $sw = [System.IO.StreamWriter]::new($pipe)
                    $sw.WriteLine("message")
                    $sw.Dispose()
                } -ArgumentList ([string]$dupPipe.DangerousGetHandle())

                $reader = [IO.StreamReader]::new($pipe)
                $actual = $reader.ReadToEnd()
                $reader.Dispose()

                $actual.Trim() | Should -Be "message"
            }
            finally {
                $session | Remove-ProcessExSession
            }
        }
        finally {
            $parentProc | Stop-Process -Force
            $pipe.Dispose()
        }
    }

    It "Sets a custom desktop" {
        $desktopName = "ProcessExDesktop"
        $desktop = [ProcessExTests.Native]::CreateDesktopEx($desktopName, "NONE",
            [ProcessEx.Security.DesktopAccessRights]::AllAccess, 128)
        try {
            $si = New-StartupInfo -Desktop $desktopName
            $session = New-ProcessExSession -StartupInfo $si -Token $token
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

    It "Sets explicit desktop same as caller" {
        $desktop = [ProcessExTests.Native]::GetThreadDesktop([ProcessExTests.Native]::GetCurrentThreadId())
        $desktopName = [ProcessExTests.Native]::GetUserObjectName($desktop)

        $si = New-StartupInfo -Desktop $desktopName
        $session = New-ProcessExSession -StartupInfo $si -Token $token
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

    It "Set explicit station\desktop with station same as caller" {
        $station = [ProcessExTests.Native]::GetProcessWindowStation()
        $stationName = [ProcessExTests.Native]::GetUserObjectName($station)

        $desktopName = "ProcessExDesktop"
        $desktop = [ProcessExTests.Native]::CreateDesktopEx($desktopName, "NONE",
            [ProcessEx.Security.DesktopAccessRights]::AllAccess, 128)
        try {
            $si = New-StartupInfo -Desktop "$stationName\$desktopName"
            $session = New-ProcessExSession -StartupInfo $si -Token $token
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

    It "Sets a custom station and desktop" {
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
            $session = New-ProcessExSession -StartupInfo $si -Token $token
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

    It "Sets explicit station and desktop same as caller" {
        $station = [ProcessExTests.Native]::GetProcessWindowStation()
        $stationName = [ProcessExTests.Native]::GetUserObjectName($station)

        $desktop = [ProcessExTests.Native]::GetThreadDesktop([ProcessExTests.Native]::GetCurrentThreadId())
        $desktopName = [ProcessExTests.Native]::GetUserObjectName($desktop)

        $si = New-StartupInfo -Desktop "$stationName\$desktopName"
        $session = New-ProcessExSession -StartupInfo $si -Token $token
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

    It "Uses process and thread security attributes" {
        # FUTURE: Make this into a cmdlet so users can easily make this
        $procSec = [ProcessEx.Security.ProcessSecurity]::new(
            (Get-ProcessEx -Id $pid).Process,
            [System.Security.AccessControl.AccessControlSections]"Access, Group, Owner")
        $procSec.AddAccessRule($procSec.AccessRuleFactory(
            [System.Security.Principal.SecurityIdentifier]::new("S-1-1-0"), # Everybody
            [ProcessEx.Security.ProcessAccessRights]::AllAccess,
            $false,
            [System.Security.AccessControl.InheritanceFlags]::None,
            [System.Security.AccessControl.PropagationFlags]::None,
            [System.Security.AccessControl.AccessControlType]::Allow))
        $procSDDL = $procSec.GetSecurityDescriptorSddlForm("Access, Group, Owner")

        $procAttributes = [ProcessEx.Security.SecurityAttributes]@{
            InheritHandle = $true
            SecurityDescriptor = $procSec
        }
        $threadAttributes = [ProcessEx.Security.SecurityAttributes]@{
            InheritHandle = $false
        }
        $projectPath = Split-Path -Path $PSScriptRoot -Parent

        $procParams = @{
            FilePath = "powershell.exe"
            ProcessAttribute = $procAttributes
            ThreadAttribute = $threadAttributes
            Token = $token
            PassThru = $true
        }
        $proc = Start-ProcessEx @procParams
        $session = New-ProcessExSession -WorkingDirectory $projectPath -Token $token
        try {
            $actualSec = [ProcessEx.Security.ProcessSecurity]::new(
                $proc.Process,
                [System.Security.AccessControl.AccessControlSections]"Access, Group, Owner")
            $actualSec.GetSecurityDescriptorSddlForm("Access, Group, Owner") | Should -Be $procSDDL

            $actual = Invoke-Command -Session $session {
                $ErrorActionPreference = 'Stop'

                Import-Module -Name .\output\ProcessEx

                Add-Type -Namespace Win32 -Name Methods -MemberDefinition @'
[DllImport("Kernel32.dll", EntryPoint = "GetProcessId", SetLastError = true)]
private static extern int NativeGetProcessId(SafeHandle Process);

public static int GetProcessId(SafeHandle process)
{
    int pid = NativeGetProcessId(process);
    if (pid == 0)
        throw new System.ComponentModel.Win32Exception();

    return pid;
}

[DllImport("Kernel32.dll", EntryPoint = "GetThreadId", SetLastError = true)]
private static extern int NativeGetThreadId(SafeHandle Thread);

public static int GetThreadId(SafeHandle thread)
{
    int pid = NativeGetThreadId(thread);
    if (pid == 0)
        throw new System.ComponentModel.Win32Exception();

    return pid;
}
'@

                $procHandle = [IntPtr][Int64]$args[0]
                $procSafeHandle = [Microsoft.Win32.SafeHandles.SafeProcessHandle]::new($procHandle, $true)
                try {
                    [Win32.methods]::GetProcessId($procSafeHandle)

                    $procSec = [ProcessEx.Security.ProcessSecurity]::new(
                        $procSafeHandle,
                        [System.Security.AccessControl.AccessControlSections]"Access, Group, Owner")
                    $procSec.GetSecurityDescriptorSddlForm("Access, Group, Owner")
                }
                finally {
                    $procSafeHandle.Dispose()
                }

                # Wasn't marked as inheritable so this is expected to fail
                $threadHandle = [IntPtr][Int64]$args[1]
                $threadSafeHandle = [Microsoft.Win32.SafeHandles.SafeProcessHandle]::new($threadHandle, $true)

                try {
                    [void][Win32.Methods]::GetThreadId($threadSafeHandle)
                }
                catch [System.ComponentModel.Win32Exception] {
                    $_.Exception.NativeErrorCode
                }

            } -ArgumentList @([string]$proc.Process.DangerousGetHandle(), [string]$proc.Thread.DangerousGetHandle())

            $actual.Count | Should -Be 3
            $actual[0] | Should -Be $proc.ProcessId
            $actual[1] | Should -Be $procSDDL
            $actual[2] | Should -Be 6 # ERROR_INVALID_HANDLE
        }
        finally {
            $session | Remove-ProcessExSession
            $proc | Stop-Process -Force
        }
    }
}
