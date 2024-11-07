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
        $outputPipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
        $inputPipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("Out", "Inheritable")

        $ptyParams = @{
            Width = 80
            Height = 80
            InputPipe = $inputPipe.ClientSafePipeHandle
            OutputPipe = $outputPipe.ClientSafePipeHandle
        }
        $pty = New-ConPTY @ptyParams
        try {
            $outputPipe.DisposeLocalCopyOfClientHandle()
            $inputPipe.DisposeLocalCopyOfClientHandle()
            $pty -is ([System.Runtime.InteropServices.SafeHandle]) | Should -Be $true

            $reader = [IO.StreamReader]::new($outputPipe)
            $writer = [IO.StreamWriter]::new($inputPipe)
            $writer.Write("echo 'hi' && exit 1`r`n")
            $writer.Flush()

            $si = New-StartupInfo -ConPTY $pty -Flags UseStdHandles
            $proc = Start-ProcessEx -FilePath cmd.exe -StartupInfo $si -Wait -PassThru

            $inputPipe.Dispose()
            $inputPipe = $Null
            $pty.Dispose()
            $pty = $null

            $out = $reader.ReadToEnd()

            $out | Should -BeLike '*hi*'
            $proc.ExitCode | Should -Be 1
        }
        finally {
            $outputPipe.Dispose()
            if ($inputPipe) { $inputPipe.Dispose() }
            if ($pty) { $pty.Dispose() }
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
