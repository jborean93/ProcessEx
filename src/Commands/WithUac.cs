using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Management.Automation;
using System.Runtime.InteropServices;
using ProcessEx.Native;
using ProcessEx.Security;

namespace ProcessEx.Commands
{
    public sealed class UacBootstrap
    {
        public static void CreateProcessForPid(
            int stdoutId,
            int stderrId,
            int stdinId,
            int processId,
            string encodedCommand)
        {
            SafeNativeHandle stdin = new SafeNativeHandle((IntPtr)stdinId, false);
            SafeNativeHandle stdout = new SafeNativeHandle((IntPtr)stdinId, false);
            SafeNativeHandle stderr = new SafeNativeHandle((IntPtr)stdinId, false);
            StartupInfo si = new StartupInfo()
            {
                ShowWindow = WindowStyle.Hide,
                StandardInput = stdin,
                StandardOutput = stdout,
                StandardError = stderr,
                InheritedHandles = new SafeHandle[] { stdin, stdout, stderr },
                ParentProcess = processId,
            };
            ProcessInfo pi = ProcessRunner.CreateProcess(
                "C:\\Windows\\System32\\whoami.exe",
                "C:\\Windows\\System32\\whoami.exe /all",
                null,
                null,
                true,
                CreationFlags.None,
                null,
                "C:\\temp",
                si,
                false,
                copyHandle: false);

            using (pi.Process)
            using (pi.Thread)
            using (SafeNativeHandle targetProcess = Kernel32.OpenProcess(processId, ProcessAccessRights.DupHandle, false))
            {
                Kernel32.DuplicateHandle(
                    Kernel32.GetCurrentProcess(),
                    pi.Process,
                    targetProcess,
                    0,
                    false,
                    Helpers.DuplicateHandleOptions.DUPLICATE_SAME_ACCESS,
                    false);
            }
        }
    }

    [Cmdlet(
        VerbsLifecycle.Invoke, "WithUac"
    )]
    [Alias("uac")]
    [OutputType(typeof(string))]
    public sealed class InvokeWithUac : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromRemainingArguments = true
        )]
        public string[] ArgumentList { get; set; }

        protected override void EndProcessing()
        {
            using AnonymousPipeServerStream stdoutPipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.None);
            using AnonymousPipeServerStream stderrPipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.None);
            using AnonymousPipeServerStream stdinPipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None);
            using StreamReader stdoutReader = new StreamReader(stdoutPipe, Console.OutputEncoding);
            using StreamReader stderrReader = new StreamReader(stderrPipe, Console.OutputEncoding);
            using StreamWriter stdinWriter = new StreamWriter(stdinPipe, Console.OutputEncoding);

            string encodedCommand = "";

            string subMethod = string.Format(@"-NoExit -NoProfile -NonInteractive -Command try {{
    Import-Module -Name '{0}'
    [ProcessEx.Commands.UacBootstrap]::CreateProcessForPid({1}, {2}, {3}, {4}, '{5}')
}}
catch {{
    Write-Host $_.Exception.ToString()
    Read-Host -Prompt 'Any key to exit'
}}",
                typeof(InvokeWithUac).Assembly.Location,
                (Int64)stdoutPipe.ClientSafePipeHandle.DangerousGetHandle(),
                (Int64)stderrPipe.ClientSafePipeHandle.DangerousGetHandle(),
                (Int64)stdinPipe.ClientSafePipeHandle.DangerousGetHandle(),
                Process.GetCurrentProcess().Id,
                encodedCommand);

            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = "pwsh.exe",
                Arguments = subMethod,
                Verb = "RunAs",
                UseShellExecute = true,
                // WindowStyle = ProcessWindowStyle.Hidden,
            };
            using Process uacProcess = Process.Start(psi);
            uacProcess.WaitForExit();

            if (uacProcess.ExitCode != 0)
            {
                throw new Exception($"Bootstrap UAC process failed with {uacProcess.ExitCode}");
            }

            stdoutPipe.DisposeLocalCopyOfClientHandle();
            stderrPipe.DisposeLocalCopyOfClientHandle();
            stdinPipe.DisposeLocalCopyOfClientHandle();

            WriteObject(stdoutReader.ReadToEnd());
        }
    }
}
