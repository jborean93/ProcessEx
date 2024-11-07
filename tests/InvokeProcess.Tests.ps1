BeforeAll {
    . ([IO.Path]::Combine($PSScriptRoot, 'common.ps1'))
}

Describe "Invoke-ProcessEx" {
    It "Invokes and captures output" {
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '[Console]::Out.WriteLine("stdout"); [Console]::Error.WriteLine("stderr"); exit 1'
            ErrorVariable = 'err'
            ErrorAction = 'SilentlyContinue'
        }
        $out = Invoke-ProcessEx @procParams

        $out | Should -Be stdout
        $err.Count | Should -Be 1
        $err[0].ToString() | Should -Be stderr
        $LASTEXITCODE | Should -Be 1
    }

    It "Redirects stdout to console" {
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '[Console]::Out.WriteLine("stdout"); [Console]::Error.WriteLine("stderr"); exit 1'
            ErrorVariable = 'err'
            ErrorAction = 'SilentlyContinue'
            RedirectStdout = 'Console'
        }
        $out = Invoke-ProcessEx @procParams

        $out | Should -BeNullOrEmpty
        $err.Count | Should -Be 1
        $err[0].ToString() | Should -Be stderr
        $LASTEXITCODE | Should -Be 1
    }

    It "Redirects stdout to null" {
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '[Console]::Out.WriteLine("stdout"); [Console]::Error.WriteLine("stderr"); exit 1'
            ErrorVariable = 'err'
            ErrorAction = 'SilentlyContinue'
            RedirectStdout = 'Null'
        }
        $out = Invoke-ProcessEx @procParams

        $out | Should -BeNullOrEmpty
        $err.Count | Should -Be 1
        $err[0].ToString() | Should -Be stderr
        $LASTEXITCODE | Should -Be 1
    }

    It "Redirects stdout to error" {
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '[Console]::Out.WriteLine("stdout"); [Console]::Error.WriteLine("stderr"); exit 1'
            ErrorVariable = 'err'
            ErrorAction = 'SilentlyContinue'
            RedirectStdout = 'Error'
        }
        $out = Invoke-ProcessEx @procParams

        $out | Should -BeNullOrEmpty
        $err.Count | Should -Be 2
        $err[0].ToString() | Should -Be stdout
        $err[1].ToString() | Should -Be stderr
        $LASTEXITCODE | Should -Be 1
    }

    It "Redirects stderr to console" {
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '[Console]::Out.WriteLine("stdout"); [Console]::Error.WriteLine("stderr"); exit 1'
            ErrorVariable = 'err'
            ErrorAction = 'SilentlyContinue'
            RedirectStderr = 'Console'
        }
        $out = Invoke-ProcessEx @procParams

        $out | Should -Be stdout
        $err.Count | Should -Be 0
        $LASTEXITCODE | Should -Be 1
    }

    It "Redirects stderr to null" {
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '[Console]::Out.WriteLine("stdout"); [Console]::Error.WriteLine("stderr"); exit 1'
            ErrorVariable = 'err'
            ErrorAction = 'SilentlyContinue'
            RedirectStderr = 'Null'
        }
        $out = Invoke-ProcessEx @procParams

        $out | Should -Be stdout
        $err.Count | Should -Be 0
        $LASTEXITCODE | Should -Be 1
    }

    It "Redirects stderr to output" {
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '[Console]::Out.WriteLine("stdout"); [Console]::Error.WriteLine("stderr"); exit 1'
            RedirectStderr = 'Output'
        }
        $out = Invoke-ProcessEx @procParams

        $out.Count | Should -Be 2
        $out | Should -Contain stdout
        $out | Should -Contain stderr
        $LASTEXITCODE | Should -Be 1
    }

    It "Swaps stdout and stderr" {
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '[Console]::Out.WriteLine("stdout"); [Console]::Error.WriteLine("stderr"); exit 1'
            ErrorVariable = 'err'
            ErrorAction = 'SilentlyContinue'
            RedirectStdout = 'Error'
            RedirectStderr = 'Output'
        }
        $out = Invoke-ProcessEx @procParams

        $out | Should -Be stderr
        $err.Count | Should -Be 1
        $err[0].ToString() | Should -Be stdout
        $LASTEXITCODE | Should -Be 1
    }

    It "Uses ConPTY" {
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '"caf$([char]0xE9)"; [Console]::Error.WriteLine("stderr"); exit 1'
            UseConPTY = $true
        }
        $out = Invoke-ProcessEx @procParams

        ($out -join "`n") | Should -BeLike "*caf$([char]0xE9)*"
        ($out -join "`n") | Should -BeLike '*stderr*'
        $err.Count | Should -Be 0
        $LASTEXITCODE | Should -Be 1
    }

    It "Outputs string with custom encoding <Encoding>" -TestCases @(
        # By well known constants
        @{ Encoding = 'ANSI'; EncodingObj = ([Text.Encoding]::GetEncoding([Globalization.CultureInfo]::CurrentCulture.TextInfo.ANSICodePage)) }
        @{ Encoding = 'BigEndianUnicode'; EncodingObj = ([Text.UnicodeEncoding]::new($true, $true)) }
        @{ Encoding = 'BigEndianUTF32'; EncodingObj = ([Text.UTF32Encoding]::new($true, $true)) }
        @{ Encoding = 'ConsoleInput'; EncodingObj = ([Console]::InputEncoding) }
        @{ Encoding = 'ConsoleOutput'; EncodingObj = ([Console]::OutputEncoding) }
        @{ Encoding = 'OEM'; EncodingObj = ([Console]::OutputEncoding) }
        @{ Encoding = 'Unicode'; EncodingObj = ([Text.Encoding]::Unicode) }
        @{ Encoding = 'UTF8'; EncodingObj = ([Text.UTF8Encoding]::new()) }
        @{ Encoding = 'UTF8Bom'; EncodingObj = ([Text.UTF8Encoding]::new($true)) }
        @{ Encoding = 'UTF8NoBom'; EncodingObj = ([Text.UTF8Encoding]::new()) }
        @{ Encoding = 'UTF32'; EncodingObj = ([Text.UTF32Encoding]::new()) }
        # By string value
        @{ Encoding = 'Windows-1252'; EncodingObj = ([Text.Encoding]::GetEncoding('Windows-1252')) }
        # By int value
        @{ Encoding = 65001; EncodingObject = ([Text.UTF8Encoding]::new()) }
        # By Encoding object
        @{ Encoding = ([Text.Encoding]::Unicode); EncodingObject = ([Text.Encoding]::Unicode) }
    ) {
        param ($Encoding, $EncodingObject)

        $text = "caf$([char]0x0E9)"

        $cmd = "`$enc = [Text.Encoding]::GetEncoding($($EncodingObject.CodePage)); `$data = `$enc.GetBytes(`"$text``n`"); for (`$i = 0; `$i -lt 2; `$i++) { [Console]::OpenStandardOutput().Write(`$data, 0, `$data.Length) }"
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', $cmd
            OutputEncoding = $Encoding
        }
        $out = Invoke-ProcessEx @procParams
        $out.Count | Should -Be 2
        $out[0] | Should -Be $text
        $out[1] | Should -Be $text
    }

    It "Outputs string with custom PSObject wrapped string encoding" {
        $text = "caf$([char]0x0E9)"

        $enc = 'Unicode' | Write-Output

        $cmd = "`$enc = [Text.Encoding]::Unicode; `$data = `$enc.GetBytes(`"$text``n`"); for (`$i = 0; `$i -lt 2; `$i++) { [Console]::OpenStandardOutput().Write(`$data, 0, `$data.Length) }"
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', $cmd
            OutputEncoding = $enc
        }
        $out = Invoke-ProcessEx @procParams
        $out.Count | Should -Be 2
        $out[0] | Should -Be $text
        $out[1] | Should -Be $text
    }

    It "Outputs string with ASCII string encoding" {
        $cmd = "`$enc = [Text.Encoding]::ASCII; `$data = `$enc.GetBytes(`"cafe`n`"); for (`$i = 0; `$i -lt 2; `$i++) { [Console]::OpenStandardOutput().Write(`$data, 0, `$data.Length) }"
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', $cmd
            OutputEncoding = 'ASCII'
        }
        $out = Invoke-ProcessEx @procParams
        $out.Count | Should -Be 2
        $out[0] | Should -Be cafe
        $out[1] | Should -Be cafe
    }

    It "Outputs string with custom encoding and -Raw" {
        $text = "caf$([char]0xE9)"
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '$data = [Text.Encoding]::Unicode.GetBytes("caf$([char]0xE9)`n"); for ($i = 0; $i -lt 2; $i++) { [Console]::OpenStandardOutput().Write($data, 0, $data.Length) }'
            OutputEncoding = 'Unicode'
            Raw = $true
        }
        try {
            $out = Invoke-ProcessEx @procParams
        }
        catch {
            Get-Error | Out-Host
            throw
        }

        $out.Count | Should -Be 1
        $out | Should -Be "$text`n$text`n"
    }

    It "Outputs as bytes" {
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '$data = [byte[]]@(0, 1, 2, 3); [Console]::OpenStandardOutput().Write($data, 0, 4)'
            OutputEncoding = 'Bytes'
        }
        $out = Invoke-ProcessEx @procParams
        $out.Count | Should -Be 4
        $out[0] | Should -BeOfType ([byte])
        $out[0] | Should -Be 0
        $out[1] | Should -BeOfType ([byte])
        $out[1] | Should -Be 1
        $out[2] | Should -BeOfType ([byte])
        $out[2] | Should -Be 2
        $out[3] | Should -BeOfType ([byte])
        $out[3] | Should -Be 3
    }

    It "Outputs as bytes and -Raw" {
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '$data = [byte[]]@(0, 1, 2, 3); [Console]::OpenStandardOutput().Write($data, 0, 4)'
            OutputEncoding = 'Bytes'
            Raw = $true
        }
        $out = Invoke-ProcessEx @procParams
        $out.Count | Should -Be 4
        , $out | Should -BeOfType ([byte[]])
        $out[0] | Should -Be 0
        $out[1] | Should -Be 1
        $out[2] | Should -Be 2
        $out[3] | Should -Be 3
    }

    It "Outputs ConPTY as bytes" {
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '"hi"'
            OutputEncoding = 'Bytes'
            UseConPTY = $true
        }
        $out = Invoke-ProcessEx @procParams
        $out | ForEach-Object {
            $_ | Should -BeOfType ([byte])
        }
        $outString = [System.Text.Encoding]::UTF8.GetString($out)
        $outString | Should -BeLike '*hi*'
    }

    It "Writes large amounts of data" {
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '"a" * 1MB'
        }
        $out = Invoke-ProcessEx @procParams
        $out | Should -Be ("a" * 1MB)
    }

    It "Writes input as a string" {
        $inputEncoding = [Console]::InputEncoding
        $expected = -join @(
            "$(-join ($inputEncoding.GetBytes('c') | ForEach-Object ToString X2))0D0A"
            "$(-join ($inputEncoding.GetBytes("$([char]0xE9)") | ForEach-Object ToString X2))0D0A"
        )

        $out = 'c', "$([char]0xE9)" | procex powershell.exe '-File' "$PSScriptRoot\HexDump.ps1"
        $out | Should -Be $expected
    }

    It "Writes input as a string array" {
        $inputEncoding = [Console]::InputEncoding
        $expected = -join @(
            "$(-join ($inputEncoding.GetBytes('c') | ForEach-Object ToString X2))0D0A"
            "$(-join ($inputEncoding.GetBytes("$([char]0xE9)") | ForEach-Object ToString X2))0D0A"
        )

        $out = @(, [string[]]@('c', "$([char]0xE9)")) | procex powershell.exe '-File' "$PSScriptRoot\HexDump.ps1"
        $out | Should -Be $expected
    }

    It "Writes input with custom encoding" {
        $out = 'c', "$([char]0xE9)" | procex powershell.exe '-File' "$PSScriptRoot\HexDump.ps1" -InputEncoding UTF8
        $out | Should -Be "630D0AC3A90D0A"
    }

    It "Writes input as a byte" {
        $out = [byte[]]@(0, 1) | procex powershell.exe '-File' "$PSScriptRoot\HexDump.ps1"
        $out | Should -Be "0001"
    }

    It "Writes input as a byte array" {
        $out = @(, [byte[]]@(0, 1)) | procex powershell.exe '-File' "$PSScriptRoot\HexDump.ps1"
        $out | Should -Be "0001"
    }

    It "Writes input as other type" {
        $out = 1, @(2, [byte]3) | procex powershell.exe '-File' "$PSScriptRoot\HexDump.ps1"
        $out | Should -Be "310D0A320D0A03"
    }

    It "Writes input with ConPTY" {
        $procParams = @{
            FilePath = 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
            UseConPTY = $true
        }
        $out = "'caf$([char]0xE9)'`r`nexit`r`n" | Invoke-ProcessEx @procParams

        ($out -join "`n") | Should -BeLike "*caf$([char]0xE9)*"
        $err.Count | Should -Be 0
    }

    It "Writes input with available data" {
        Function Test-Function {
            Start-Sleep -Seconds 5
            'data'
        }

        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '"initial"; $input'
        }
        $out = Test-Function | Invoke-ProcessEx @procParams
        $out.Count | Should -Be 2
        $out[0] | Should -Be initial
        $out[1] | Should -Be data
    }

    It "Runs with custom working directory" {
        $out = Invoke-ProcessEx powershell.exe '-command' '$pwd.Path' -WorkingDirectory C:\Windows
        $out | Should -Be C:\Windows
    }

    It "Runs with custom token" -Skip:(Get-ProcessPrivilege -Name SeAssignPrimaryTokenPrivilege).IsRemoved {
        $domain = $env:COMPUTERNAME
        $username = "ProcessEx-Test"
        $password = "Password123!"
        $token = $null

        $userParams = @{
            Name = $username
            Password = (ConvertTo-SecureString -AsPlainText -Force -String $password)
            Description = "Test user for ProcessEx with higher privileges"
            PasswordNeverExpires = $true
            UserMayNotChangePassword = $true
            GroupMembership = "Administrators"
        }
        $user = New-LocalAccount @userParams
        try {
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

            $out = Invoke-ProcessEx whoami -Token $token
            $out | Should -Be "$env:COMPUTERNAME\ProcessEx-Test"
        }
        finally {
            if ($token) { $token.Dispose() }

            ([IO.Path]::GettempPath()), 'C:\Windows\TEMP' | ForEach-Object {
                $acl = Get-Acl -LiteralPath $_
                $acl.PurgeAccessRules($user)
                Set-Acl -LiteralPath $_ -AclObject $acl
            }

            Remove-LocalAccount -Account $user
        }
    }

    It "Runs with custom ConPTY dimensions" {
        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '"Height=$($host.UI.RawUI.BufferSize.Height)"; "Width=$($host.UI.RawUI.BufferSize.Width)"'
            UseConPTY = $true
            ConPTYHeight = 10
            ConPTYWidth = 20
        }
        $out = Invoke-ProcessEx @procParams

        ($out -join "`n") | Should -BeLike "*Height=10*"
        ($out -join "`n") | Should -BeLike "*Width=20*"
        $err.Count | Should -Be 0
    }

    It "Inherits env var from parent" {
        $env:ProcessExTest = 'abc'
        try {
            $out = Invoke-ProcessEx powershell.exe '$env:ProcessExTest'
            $out | Should -Be abc
        }
        finally {
            $env:ProcessExTest = $null
        }
    }

    It "Runs with custom environment" {
        $envBlock = [Environment]::GetEnvironmentVariables()
        $envBlock['ProcessExTest'] = 'foo'

        $env:ProcessExTest = 'bar'
        try {
            $out = Invoke-ProcessEx powershell.exe '$env:ProcessExTest' -Environment $envBlock
            $out | Should -Be foo
        }
        finally {
            $env:ProcessExTest = $null
        }
    }

    It "Runs with new environment" {
        $env:ProcessExTest = 'abc'
        try {
            $out = Invoke-ProcessEx powershell.exe '$env:ProcessExTest' -UseNewEnvironment
            $out | Should -BeNullOrEmpty
        }
        finally {
            $env:ProcessExTest = $null
        }
    }

    It "Tests handle inheritance" {
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new('In', 'Inheritable')
        try {
            $procParams = @{
                FilePath = 'powershell.exe'
                ArgumentList = '-Command', "`$pipe = [System.IO.Pipes.AnonymousPipeClientStream]::new('Out', '$($pipe.GetClientHandleAsString())'); `$pipe.WriteByte(1); `$pipe.Dispose()"
                ErrorAction = 'Stop'
            }

            Invoke-ProcessEx @procParams
            $pipe.DisposeLocalCopyOfClientHandle()

            $pipe.ReadByte() | Should -Be 1
        }
        finally {
            $pipe.Dispose()
        }
    }

    It "Disables inheritance with redirected output" {
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new('In', 'Inheritable')
        try {
            $procParams = @{
                FilePath = 'powershell.exe'
                ArgumentList = '-Command', "'test'; [System.IO.Pipes.AnonymousPipeClientStream]::new('Out', '$($pipe.GetClientHandleAsString())') | ForEach-Object Dispose"
                DisableInheritance = $true
                ErrorAction = 'SilentlyContinue'
                ErrorVariable = 'err'
                Raw = $true
            }

            $out = Invoke-ProcessEx @procParams
            $pipe.DisposeLocalCopyOfClientHandle()

            $out | Should -Be "test`r`n"
            $err.Count | Should -Be 1
            $err[0].ToString() | Should -BeLike "*Invalid pipe handle*"
        }
        finally {
            $pipe.Dispose()
        }
    }

    It "Disabled inheritance with ConPTY" {
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new('In', 'Inheritable')
        try {
            $procParams = @{
                FilePath = 'powershell.exe'
                ArgumentList = '-Command', "[System.IO.Pipes.AnonymousPipeClientStream]::new('Out', '$($pipe.GetClientHandleAsString())') | ForEach-Object Dispose"
                DisableInheritance = $true
                UseConPTY = $true
                Raw = $true
            }

            $out = Invoke-ProcessEx @procParams
            $pipe.DisposeLocalCopyOfClientHandle()

            $out | Should -BeLike "*Invalid pipe handle*"
        }
        finally {
            $pipe.Dispose()
        }
    }

    It "Disables inheritance with no redirected handles" {
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new('In', 'Inheritable')
        $tmpFile = [System.IO.Path]::GetTempFileName()
        try {
            $procParams = @{
                FilePath = 'powershell.exe'
                ArgumentList = '-Command', "try { [System.IO.Pipes.AnonymousPipeClientStream]::new('Out', '$($pipe.GetClientHandleAsString())') | ForEach-Object Dispose } catch { Set-Content -LiteralPath '$tmpFile' -Value `$_.Exception.Message }"
                DisableInheritance = $true
                RedirectStdout = 'Console'
                RedirectStderr = 'Console'
            }

            Invoke-ProcessEx @procParams
            $pipe.DisposeLocalCopyOfClientHandle()

            Test-Path -LiteralPath $tmpFile | Should -BeTrue
            Get-Content -LiteralPath $tmpFile | Should -BeLike "*Invalid pipe handle*"
        }
        finally {
            if (Test-Path -LiteralPath $tmpFile) {
                Remove-Item -Path $tmpFile -Force
            }
            $pipe.Dispose()
        }
    }

    It "Runs with command line parameter" {
        $procParams = @{
            CommandLine = 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -Command "[Environment]::UserName"'
        }
        $out = Invoke-ProcessEx @procParams
        $out | Should -Be ([Environment]::UserName)
    }

    It "Runs with command line and application name" {
        $procParams = @{
            ApplicationName = 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
            CommandLine = 'ignored.exe -Command "[Environment]::UserName"'
        }
        $out = Invoke-ProcessEx @procParams
        $out | Should -Be ([Environment]::UserName)
    }

    It "Runs an executable in current directory <Path>" -TestCases @(
        @{ Path = '.\whoami.exe'; WorkingDirectory = 'C:\Windows\System32' }
        @{ Path = './whoami.exe'; WorkingDirectory = 'C:\Windows\System32' }
        @{ Path = '.\..\System32\whoami.exe'; WorkingDirectory = 'C:\Windows\System32' }
        @{ Path = './../System32/whoami.exe'; WorkingDirectory = 'C:\Windows\System32' }
        @{ Path = '..\whoami.exe'; WorkingDirectory = 'C:\Windows\System32\WindowsPowerShell' }
        @{ Path = '../whoami.exe'; WorkingDirectory = 'C:\Windows\System32\WindowsPowerShell' }
    ) {
        param ($Path, $WorkingDirectory)

        $expected = whoami.exe
        $out = Invoke-ProcessEx -FilePath $Path -WorkingDirectory $WorkingDirectory

        $out | Should -Be $expected
    }

    It "Stops a still running process with the stop request with alias <UseAlias>" -TestCases @(
        @{ UseAlias = $false }
        @{ UseAlias = $true }
    ) {
        param ($UseAlias)

        $cmdletName = if ($UseAlias) {
            'procex'
        }
        else {
            'Invoke-ProcessEx'
        }

        $procParams = @{
            FilePath = 'powershell.exe'
            ArgumentList = '-Command', 'Start-Sleep -Seconds 60'
        }

        $projectPath = Split-Path -Path $PSScriptRoot -Parent
        $ps = [PowerShell]::Create()
        [void]$ps.AddCommand("Import-Module").AddParameter("Name", "$projectPath\output\ProcessEx").AddStatement()
        $task = $ps.AddCommand($cmdletName).AddParameters($procParams).BeginInvoke()
        Start-Sleep -Seconds 5

        $start = Get-Date
        $ps.Stop()
        $ps.EndInvoke($task)
        $end = (Get-Date) - $start
        $end.TotalSeconds | Should -BeLessThan 50
    }

    It "Invokes with custom parent process" {
        $proc = Start-Process powershell.exe -PassThru -WindowStyle Hidden
        try {
            $si = New-StartupInfo -ParentProcess $proc
            $procParams = @{
                FilePath = 'powershell.exe'
                ArgumentList = '-command', '(Get-CimInstance -ClassName Win32_Process -Filter "ProcessId=$pid").ParentProcessId'
                StartupInfo = $si
            }

            $out = Invoke-ProcessEx @procParams
            $out | Should -Be $proc.Id
        }
        finally {
            $proc | Stop-Process
        }
    }

    It "Fails with invalid encoding type" {
        $ex = { Invoke-ProcessEx whoami -OutputEncoding $true } | Should -Throw -PassThru
        [string]$ex | Should -BeLike "*Could not convert input 'True' to a valid Encoding object*"
    }

    It "Fails if -InputEncoding is set to Bytes" {
        $procParams = @{
            FilePath = 'whoami'
            InputEncoding = 'Bytes'
        }
        $err = { Invoke-ProcessEx @procParams } | Should -Throw -PassThru
        $err.Exception.Message | Should -Be "Cannot use -InputEncoding Bytes, the input must have an encoding set."
    }

    It "Fails if ConPTY handle is set" {
        $outputPipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $inputPipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("Out", "Inheritable")

        $pty = New-ConPTY -Width 60 -Height 80 -InputPipe $inputPipe.ClientSafePipeHandle -OutputPipe $outputPipe.ClientSafePipeHandle
        try {
            $outputPipe.DisposeLocalCopyOfClientHandle()
            $inputPipe.DisposeLocalCopyOfClientHandle()

            $si = New-StartupInfo -ConPTY $pty
            $procParams = @{
                FilePath = 'whoami'
                StartupInfo = $si
            }
            $err = { Invoke-ProcessEx @procParams } | Should -Throw -PassThru
            $err.Exception.Message | Should -Be "Cannot use -StartupInfo with a ConPTY handle."
        }
        finally {
            $pty.Dispose()
            $outputPipe.Dispose()
            $inputPipe.Dispose()
        }
    }

    It "Fails if any stdio handle is set" {
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")

        try {
            $pipe.DisposeLocalCopyOfClientHandle()

            $si = New-StartupInfo -StandardOutput $pipe.ClientSafePipeHandle
            $procParams = @{
                FilePath = 'whoami'
                StartupInfo = $si
            }
            $err = { Invoke-ProcessEx @procParams } | Should -Throw -PassThru
            $err.Exception.Message | Should -Be "Cannot use -StartupInfo with Standard Input, Output, or Error handles."
        }
        finally {
            $pipe.Dispose()
        }
    }

    It "Fails if inheritance is disabled with explicit inherited handles" {
        $fs = [System.IO.File]::OpenRead($PSCommandPath)
        $si = New-StartupInfo -InheritedHandle $fs.SafeFileHandle
        $procParams = @{
            FilePath = 'whoami'
            StartupInfo = $si
            DisableInheritance = $true
        }
        $err = { Invoke-ProcessEx @procParams } | Should -Throw -PassThru
        $fs.Dispose()
        $err.Exception.Message | Should -Be "Cannot -DisableInheritance with explicit inherited handles in StartupInfo."
    }

    It "Fails if -Environment and -UseNewEnvironment is set" {
        $procParams = @{
            FilePath = 'whoami'
            Environment = @{foo = 'bar' }
            UseNewEnvironment = $true
        }
        $err = { Invoke-ProcessEx @procParams } | Should -Throw -PassThru
        $err.Exception.Message | Should -Be "Cannot use -Environment with -UseNewEnvironment."
    }

    It "Fails if using custom parent process with console redirection" {
        $proc = Start-Process powershell.exe -PassThru -WindowStyle Hidden
        try {
            $si = New-StartupInfo -ParentProcess $proc
            $procParams = @{
                FilePath = 'powershell.exe'
                StartupInfo = $si
                RedirectStdout = 'Console'
            }

            $err = { Invoke-ProcessEx @procParams } | Should -Throw -PassThru
            $err.Exception.Message | Should -Be "Invoke-ProcessWith cannot redirect stdout/stderr to the console when using a custom parent process."
        }
        finally {
            $proc | Stop-Process
        }
    }
}

Describe "Invoke-ProcessWith" {
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

    It "Invokes and captures output with token" {
        $procParams = @{
            Token = $token
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '[Console]::Out.WriteLine(([Environment]::UserName)); [Console]::Error.WriteLine("stderr"); exit 1'
            ErrorVariable = 'err'
            ErrorAction = 'SilentlyContinue'
        }
        $out = Invoke-ProcessWith @procParams

        $out | Should -Be $username
        $err.Count | Should -Be 1
        $err[0].ToString() | Should -Be stderr
        $LASTEXITCODE | Should -Be 1
    }

    It "Invokes and captures output with credential with domain <WithDomain>" -TestCases @(
        @{ WithDomain = $false }
        @{ WithDomain = $true }
    ) {
        param ($WithDomain)

        $cred = if ($WithDomain) {
            [PSCredential]::new("$env:COMPUTERNAME\$username", $credential.Password)
        }
        else {
            $credential
        }
        $procParams = @{
            Credential = $cred
            FilePath = 'powershell.exe'
            ArgumentList = '-command', '[Console]::Out.WriteLine(([Environment]::UserName)); [Console]::Error.WriteLine("stderr"); exit 1'
            ErrorVariable = 'err'
            ErrorAction = 'SilentlyContinue'
        }
        $out = Invoke-ProcessWith @procParams

        $out | Should -Be $username
        $err.Count | Should -Be 1
        $err[0].ToString() | Should -Be stderr
        $LASTEXITCODE | Should -Be 1
    }

    It "Invokes and captures output with credential -NetCredentialsOnly" {
        $cred = [PSCredential]::new('foo@bar.com', ('test' | ConvertTo-SecureString -AsPlainText -Force))
        $procParams = @{
            Credential = $cred
            FilePath = 'powershell.exe'
            ArgumentList = '-Command', '[Environment]::UserName'
            NetCredentialsOnly = $true
        }
        $out = Invoke-ProcessWith @procParams

        $out | Should -Be ([Environment]::UserName)
        $LASTEXITCODE | Should -Be 0
    }

    It "Invokes with input and output" {
        $out = 'foo', 'bar' | Invoke-ProcessWith powershell.exe '-command' '$input' -Token $token
        $out.Count | Should -Be 2
        $out[0] | Should -Be foo
        $out[1] | Should -Be bar
    }

    It "Invokes with no profile" {
        $out = Invoke-ProcessWith powershell.exe '-command' '$env:USERPROFILE' -Credential $credential
        $out | Should -Be C:\Users\Default
    }

    It "Invokes with a profile" {
        $out = Invoke-ProcessWith powershell.exe '-command' '$env:USERPROFILE' -Credential $credential -WithProfile
        $out | Should -Not -Be C:\Users\Default
    }

    It "Fails if -Credential and -Token is set" {
        $procParams = @{
            FilePath = 'whoami'
            Credential = $credential
            Token = $token
        }
        $err = { Invoke-ProcessWith @procParams } | Should -Throw -PassThru
        $err.Exception.Message | Should -Be "Cannot set -Token and -Credential together."
    }

    It "Fails if stdout is redirected to the Console" {
        $procParams = @{
            FilePath = 'whoami'
            Token = $token
            RedirectStdout = 'Console'
        }
        $err = { Invoke-ProcessWith @procParams } | Should -Throw -PassThru
        $err.Exception.Message | Should -Be "Invoke-ProcessWith cannot redirect stdout/stderr to the console."
    }

    It "Fails if stderr is redirected to the Console" {
        $procParams = @{
            FilePath = 'whoami'
            Token = $token
            RedirectStderr = 'Console'
        }
        $err = { Invoke-ProcessWith @procParams } | Should -Throw -PassThru
        $err.Exception.Message | Should -Be "Invoke-ProcessWith cannot redirect stdout/stderr to the console."
    }
}

Describe "Encoding completions" {
    It "Completes -OutputEncoding with no value" {
        $actual = Complete 'Invoke-ProcessEx -OutputEncoding '
        $actual.Count | Should -Be 10
        $actual[0].CompletionText | Should -Be UTF8
        $actual[1].CompletionText | Should -Be ConsoleInput
        $actual[2].CompletionText | Should -Be ConsoleOutput
        $actual[3].CompletionText | Should -Be ASCII
        $actual[4].CompletionText | Should -Be ANSI
        $actual[5].CompletionText | Should -Be Bytes
        $actual[6].CompletionText | Should -Be OEM
        $actual[7].CompletionText | Should -Be Unicode
        $actual[8].CompletionText | Should -Be UTF8Bom
        $actual[9].CompletionText | Should -Be UTF8NoBom
    }

    It "Completes -OutputEncoding with partial match" {
        $actual = Complete 'Invoke-ProcessEx -OutputEncoding UT'
        $actual.Count | Should -Be 3
        $actual[0].CompletionText | Should -Be UTF8
        $actual[1].CompletionText | Should -Be UTF8Bom
        $actual[2].CompletionText | Should -Be UTF8NoBom
    }

    It "Completes -OutputEncoding with partial match and wildcard" {
        $actual = Complete 'Invoke-ProcessEx -OutputEncoding UT*'
        $actual.Count | Should -Be 3
        $actual[0].CompletionText | Should -Be UTF8
        $actual[1].CompletionText | Should -Be UTF8Bom
        $actual[2].CompletionText | Should -Be UTF8NoBom
    }
}
