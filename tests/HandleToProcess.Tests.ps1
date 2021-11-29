BeforeAll {
    . ([IO.Path]::Combine($PSScriptRoot, 'common.ps1'))
}

Describe "Copy-HandleToProcess" {
    It "Copies handle to process we created" {
        $session = New-ProcessExSession
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None")
        try {
            $dupPipe = Copy-HandleToProcess -Handle $pipe.ClientSafePipeHandle -Process $session.Process
            $pipe.DisposeLocalCopyOfClientHandle()

            Invoke-Command $session {
                $pipe = [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[0])
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
            $pipe.Dispose()
        }
    }

    It "Copies handle to process using Process obj" {
        $session = New-ProcessExSession
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None")
        try {
            $proc = Get-Process -Id $session.Process.ProcessId
            $dupPipe = Copy-HandleToProcess -Handle $pipe.ClientSafePipeHandle -Process $proc
            $pipe.DisposeLocalCopyOfClientHandle()

            Invoke-Command $session {
                $pipe = [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[0])
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
            $pipe.Dispose()
        }
    }

    It "Dispose handle with -OwnHandle" {
        $session = New-ProcessExSession
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None")
        try {
            $proc = Get-Process -Id $session.Process.ProcessId
            $dupPipe = Copy-HandleToProcess -Handle $pipe.ClientSafePipeHandle -Process $proc -OwnHandle
            $pipe.DisposeLocalCopyOfClientHandle()
            $dupPipe.Dispose()

            $actual = Invoke-Command $session {
                # Will have failed because it was disposed already
                $failed = $false
                try {
                    [System.IO.Pipes.AnonymousPipeClientStream]::new("Out", $args[0])
                }
                catch {
                    $failed = $true
                }
                $failed
            } -ArgumentList ([string]$dupPipe.DangerousGetHandle())

            $actual | Should -Be $true
        }
        finally {
            $session | Remove-ProcessExSession
            $pipe.Dispose()
        }
    }

    It "Fails to open handle to target process" {
        $pipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None")
        try {
            $err = $null
            $actual = Copy-HandleToProcess -Handle $pipe.ClientSafePipeHandle -Process 0 -ErrorAction SilentlyContinue -ErrorVariable err
            $err.Count | Should -Be 1
            [string]$err[0] | Should -BeLike "Failed to open process to duplicate handle *"

        }
        finally {
            $pipe.Dispose()
        }
    }

    It "Fails to duplicate handle" {
        $fakeHandle = [Microsoft.Win32.SafeHandles.SafeFileHandle]::new([IntPtr]::Zero, $false)
        $err = $null
        $actual = Copy-HandleToProcess -Handle $fakeHandle -Process $pid -ErrorAction SilentlyContinue -ErrorVariable err
        $err.Count | Should -Be 1
        [string]$err[0] | Should -BeLike "Failed to duplicate handle *"
    }
}
