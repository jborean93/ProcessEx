. ([IO.Path]::Combine($PSScriptRoot, 'common.ps1'))

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

    It "Creates ConPTYfor process" {
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

    }

    It "Resizes PTY size" {

    }

    It "Fails to create with invalid handles" {

    }
}
