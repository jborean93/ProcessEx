BeforeAll {
    . ([IO.Path]::Combine($PSScriptRoot, 'common.ps1'))
}

Describe "ConPTY" {
    BeforeEach {
        $fsInPath = Join-Path $TestDrive 'ConPTY-in.test'
        $fsOutPath = Join-Path $TestDrive 'ConPTY-out.ptest'
        [IO.File]::WriteAllText($fsInPath, "")

        $fsIn = [IO.File]::Open($fsInPath, "Open", "Read", "ReadWrite")
        $fsOut = [IO.File]::Open($fsOutPath, "Create", "Write", "ReadWrite")
    }

    AfterEach {
        $fsIn.Dispose()
        $fsOut.Dispose()
    }

    It "Creates ConPTY for process" {
        $fs = [IO.File]::Open($fsInPath, "Open", "Write", "ReadWrite")
        $sw = [IO.StreamWriter]::new($fs)
        $sw.WriteLine("echo 'hi' && exit 1")
        $sw.Dispose()

        $ptyParams = @{
            Width = 10
            Height = 20
            InputPipe = $fsIn.SafeFileHandle
            OutputPipe = $fsOut.SafeFileHandle
        }
        $pty = New-ConPTY @ptyParams
        try {
            $pty -is ([System.Runtime.InteropServices.SafeHandle]) | Should -Be $true

            $si = New-StartupInfo -ConPTY $pty
            $proc = Start-ProcessEx -FilePath cmd.exe -StartupInfo $si -Wait -PassThru

            Get-Content -LiteralPath $fsOutPath -Raw | Should -Not -Be ""
            $proc.ExitCode | Should -Be 1
        }
        finally {
            $pty.Dispose()
        }
    }

    It "Creates PTY that inherits cursor" {
        $ptyParams = @{
            Width = 10
            Height = 20
            InputPipe = $fsIn.SafeFileHandle
            OutputPipe = $fsOut.SafeFileHandle
            InheritCursor = $true
        }
        $pty = New-ConPTY @ptyParams
        $pty -is ([System.Runtime.InteropServices.SafeHandle]) | Should -Be $true
        $pty.Dispose()
    }

    It "Fails to create with invalid handles" {
        $nullHandle = [Microsoft.Win32.SafeHandles.SafeFileHandle]::new([IntPtr]::Zero, $false)
        $err = $null
        $ptr = New-ConPTY -Width 0 -Height 0 -InputPipe $nullHandle -OutputPipe $nullHandle -ErrorAction SilentlyContinue -ErrorVariable err
        $ptr | Should -Be $null
        $err.Count | Should -Be 1
        [string]$err[0] | Should -BeLike "Failed to create psuedo console handle *"
    }

    It "Resizes PTY size" {
        $ptyParams = @{
            Width = 10
            Height = 20
            InputPipe = $fsIn.SafeFileHandle
            OutputPipe = $fsOut.SafeFileHandle
        }
        $pty = New-ConPTY @ptyParams
        try {
            $pty -is ([System.Runtime.InteropServices.SafeHandle]) | Should -Be $true

            Resize-ConPTY -ConPTY $pty -Width 20 -Height 40
        }
        finally {
            $pty.Dispose()
        }
    }

    It "Resize fail invalid handle" {
        $nullHandle = [Microsoft.Win32.SafeHandles.SafeFileHandle]::new([IntPtr]::Zero, $false)
        $err = $null
        Resize-ConPTY -ConPTY $nullHandle -Width 20 -Height 40 -ErrorAction SilentlyContinue -ErrorVariable err

        $err.Count | Should -Be 1
        [string]$err[0] | Should -BeLike "Failed to resize psuedo console handle *"
    }
}
