using Microsoft.Win32.SafeHandles;
using ProcessEx.Native;
using ProcessEx.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace ProcessEx.Commands
{
    public sealed class UacBootstrap
    {
        internal const string ID_EXCEPTION = "EXCEPTION";
        internal const string ID_PROC_HANDLE = "PROC_HANDLE";
        internal const string ID_THREAD_HANDLE = "THREAD_HANDLE";
        internal const string ID_START = "START";

        public static void RespawnUnderProcess(int processId, IntPtr inPipeId, IntPtr outPipeId)
        {
            UTF8Encoding utf8 = new UTF8Encoding(false);

            Console.WriteLine("Getting current process");
            using SafeNativeHandle currentProcess = Kernel32.GetCurrentProcess();
            Console.WriteLine("Getting target process {0}", processId);
            using SafeNativeHandle targetProcess = Kernel32.OpenProcess(
                processId,
                ProcessAccessRights.DupHandle,
                false);

            Console.WriteLine("Duplicating in handle 0x{0:X8}", (Int64)inPipeId);
            using SafeDuplicateHandle inPipeHandle = Kernel32.DuplicateHandle(
                targetProcess,
                new SafeNativeHandle(inPipeId, false),
                currentProcess,
                0,
                false,
                Helpers.DuplicateHandleOptions.DUPLICATE_SAME_ACCESS,
                true);
            Console.WriteLine("Creating in pipe from 0x{0:X8}", (Int64)inPipeHandle.DangerousGetHandle());
            using AnonymousPipeClientStream inPipe = new AnonymousPipeClientStream(
                PipeDirection.In,
                new SafePipeHandle(inPipeHandle.DangerousGetHandle(), false));
            Console.WriteLine("Creating in pipe reader");
            using StreamReader inReader = new StreamReader(inPipe, utf8);

            Console.WriteLine("Duplicating out handle 0x{0:X8}", (Int64)inPipeId);
            using SafeDuplicateHandle outPipeHandle = Kernel32.DuplicateHandle(
                targetProcess,
                new SafeNativeHandle(outPipeId, false),
                currentProcess,
                0,
                false,
                Helpers.DuplicateHandleOptions.DUPLICATE_SAME_ACCESS,
                true);
            Console.WriteLine("Creating out pipe from 0x{0:X8}", (Int64)outPipeHandle.DangerousGetHandle());
            using AnonymousPipeClientStream outPipe = new AnonymousPipeClientStream(
                PipeDirection.Out,
                new SafePipeHandle(outPipeHandle.DangerousGetHandle(), false));
            Console.WriteLine("Creating out pipe writer");
            using StreamWriter outWriter = new StreamWriter(outPipe, utf8);
            outWriter.AutoFlush = true;

            try
            {
                Console.WriteLine("Getting current process token");
                using SafeNativeHandle currentToken = Advapi32.OpenProcessToken(currentProcess,
                    TokenAccessLevels.AllAccess);

                Console.WriteLine("Writing start");
                outWriter.WriteLine(ID_START);

                Console.WriteLine("Reading stdout pipe id");
                Int64 stdoutId = Int64.Parse(inReader.ReadLine() ?? "");
                Console.WriteLine("Received stdout pipe id 0x{0:X8}", stdoutId);

                Console.WriteLine("Reading stderr pipe id");
                Int64 stderrId = Int64.Parse(inReader.ReadLine() ?? "");
                Console.WriteLine("Received stderr pipe id 0x{0:X8}", stderrId);

                Console.WriteLine("Reading stdin pipe id");
                Int64 stdinId = Int64.Parse(inReader.ReadLine() ?? "");
                Console.WriteLine("Received stdin pipe id 0x{0:X8}", stdinId);

                Console.WriteLine("Reading lpApplicationName length");
                string appNameLengthRaw = inReader.ReadLine() ?? "";
                Console.WriteLine("Received lpApplicationName length {0}", appNameLengthRaw);

                int appNameLength = int.Parse(appNameLengthRaw);
                string? appName = null;
                if (appNameLength > 0)
                {
                    char[] buffer = new char[appNameLength];
                    int read = 0;
                    while (read < buffer.Length)
                    {
                        read += inReader.ReadBlock(buffer, read, buffer.Length - read);
                    }
                    appName = new string(buffer);
                    Console.WriteLine("Received lpApplicationName: {0}", appName);
                }

                Console.WriteLine("Reading lpCommandLine length");
                string cmdLineLengthRaw = inReader.ReadLine() ?? "";
                Console.WriteLine("Received lpCommandLine length {0}", cmdLineLengthRaw);

                int cmdLineLength = int.Parse(cmdLineLengthRaw);
                string? cmdLine = null;
                if (cmdLineLength > 0)
                {
                    char[] buffer = new char[cmdLineLength];
                    int read = 0;
                    while (read < buffer.Length)
                    {
                        read += inReader.ReadBlock(buffer, read, buffer.Length - read);
                    }
                    cmdLine = new string(buffer);
                    Console.WriteLine("Received lpCommandLine: {0}", cmdLine);
                }

                SafeNativeHandle stdout = new SafeNativeHandle((IntPtr)stdoutId, false);
                SafeNativeHandle stderr = new SafeNativeHandle((IntPtr)stderrId, false);
                SafeNativeHandle stdin = new SafeNativeHandle((IntPtr)stdinId, false);
                StartupInfo si = new StartupInfo()
                {
                    StandardInput = stdin,
                    StandardOutput = stdout,
                    StandardError = stderr,
                    ParentProcess = processId,
                };
                Console.WriteLine("Starting process '{appName}' '{cmdLine}'");
                ProcessInfo pi = ProcessRunner.CreateProcessAsUser(
                    currentToken,
                    appName,
                    cmdLine,
                    null,
                    null,
                    true,
                    CreationFlags.Suspended,
                    null,
                    null,
                    si,
                    false,
                    copyHandle: false);

                using (pi.Process)
                using (pi.Thread)
                {
                    Console.WriteLine("Process started {0}", pi.ProcessId);

                    Console.WriteLine("Duplicating process handle 0x{0:X8}", (Int64)pi.Process.DangerousGetHandle());
                    SafeDuplicateHandle procCopy = Kernel32.DuplicateHandle(
                        currentProcess,
                        pi.Process,
                        targetProcess,
                        0,
                        false,
                        Helpers.DuplicateHandleOptions.DUPLICATE_SAME_ACCESS,
                        false);
                    Console.WriteLine("Duplicated process handle 0x{0:X8}", (Int64)procCopy.DangerousGetHandle());
                    outWriter.WriteLine(ID_PROC_HANDLE);
                    outWriter.WriteLine(procCopy.DangerousGetHandle());

                    Console.WriteLine("Duplicating thread handle 0x{0:X8}", (Int64)pi.Thread.DangerousGetHandle());
                    SafeDuplicateHandle threadCopy = Kernel32.DuplicateHandle(
                        currentProcess,
                        pi.Thread,
                        targetProcess,
                        0,
                        false,
                        Helpers.DuplicateHandleOptions.DUPLICATE_SAME_ACCESS,
                        false);
                    Console.WriteLine("Duplicated thread handle 0x{0:X8}", (Int64)procCopy.DangerousGetHandle());
                    outWriter.WriteLine(ID_THREAD_HANDLE);
                    outWriter.WriteLine(threadCopy.DangerousGetHandle());

                    Console.WriteLine("Waiting for signal to exit");
                    inReader.ReadLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed: {0}\n{1}", e.Message, e.ToString());
                outWriter.WriteLine(ID_EXCEPTION);
                outWriter.WriteLine(e.ToString());
                throw;
            }

            Console.WriteLine("Done");
            // Console.ReadKey();
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
        public string[] ArgumentList { get; set; } = Array.Empty<string>();

        [DllImport("Kernel32.dll", SetLastError = true)]
        internal static extern bool CreatePipe(
            out SafeFileHandle hReadPipe,
            out SafeFileHandle hWritePipe,
            ref Helpers.SECURITY_ATTRIBUTES lpPipeAttributes,
            int nSize);

        protected override void EndProcessing()
        {
            CommandInfo? cmdInfo = SessionState.InvokeCommand
                .GetCommands(ArgumentList[0], CommandTypes.Application, false)
                .FirstOrDefault();
            if (cmdInfo == null)
            {
                string msg = @$"The term '{ArgumentList[0]}' is not recognized as an executable program.
Check the spelling of the name, or if a path was included, verify that the path is correct and try again.";
                ErrorRecord err = new ErrorRecord(
                    new CommandNotFoundException(msg),
                    "CommandNotFoundException",
                    ErrorCategory.ObjectNotFound,
                    ArgumentList[0]);
                ThrowTerminatingError(err);
                return;
            }
            string appName = cmdInfo.Source;

            List<string> arguments = new List<string>()
            {
                $"\"{appName}\"",
            };
            for (int i = 1; i < ArgumentList.Length; i++)
            {
                string arg = ArgumentList[i];
                arguments.Add(arg);
            }
            string cmdLine = string.Join(" ", arguments);

            UTF8Encoding utf8 = new UTF8Encoding(false);

            // Helpers.SECURITY_ATTRIBUTES secAttr = new Helpers.SECURITY_ATTRIBUTES()
            // {
            //     bInheritHandle = true,
            // };

            // CreatePipe(out var stdoutPipeReader, out var stdoutPipeWriter, ref secAttr, 0);
            // CreatePipe(out var stderrPipeReader, out var stderrPipeWriter, ref secAttr, 0);
            // CreatePipe(out var stdinPipeReader, out var stdinPipeWriter, ref secAttr, 0);

            using AnonymousPipeServerStream inPipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.None);
            using AnonymousPipeServerStream outPipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None);
            using AnonymousPipeServerStream stdoutPipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            using AnonymousPipeServerStream stderrPipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            using AnonymousPipeServerStream stdinPipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            using StreamReader inReader = new StreamReader(inPipe, utf8);
            using StreamWriter outWriter = new StreamWriter(outPipe, utf8);
            // using FileStream stdoutFS = new FileStream(stdoutPipeReader, FileAccess.Read);
            // using FileStream stderrFS = new FileStream(stderrPipeReader, FileAccess.Read);
            // using FileStream stdinFS = new FileStream(stdinPipeWriter, FileAccess.Write);
            using StreamReader stdoutReader = new StreamReader(stdoutPipe, Console.OutputEncoding);
            using StreamReader stderrReader = new StreamReader(stderrPipe, Console.OutputEncoding);
            using StreamWriter stdinWriter = new StreamWriter(stdinPipe, Console.OutputEncoding);
            // using StreamReader stdoutReader = new StreamReader(stdoutFS, Console.OutputEncoding);
            // using StreamReader stderrReader = new StreamReader(stderrFS, Console.OutputEncoding);
            // using StreamWriter stdinWriter = new StreamWriter(stdinFS, Console.OutputEncoding);

            outWriter.AutoFlush = true;
            stdinWriter.AutoFlush = true;

            string subMethod = string.Format(@"-NoProfile -Command try {{
    Import-Module -Name '{0}'
    [ProcessEx.Commands.UacBootstrap]::RespawnUnderProcess({1}, {2}, {3})
}}
catch {{
    Write-Host $_.Exception.ToString()
    Read-Host -Prompt 'Any key to exit'
}}",
                typeof(InvokeWithUac).Assembly.Location,
                Process.GetCurrentProcess().Id,
                (Int64)outPipe.ClientSafePipeHandle.DangerousGetHandle(),
                (Int64)inPipe.ClientSafePipeHandle.DangerousGetHandle());

            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = "pwsh.exe",
                Arguments = subMethod,
                Verb = "RunAs",
                UseShellExecute = true,
                // WindowStyle = ProcessWindowStyle.Hidden,
            };
            using Process uacProcess = Process.Start(psi);

            string start = inReader.ReadLine() ?? "";
            if (start == UacBootstrap.ID_EXCEPTION)
            {
                throw new Exception(inReader.ReadLine() ?? "");
            }

            // outWriter.WriteLine(stdoutPipeWriter.DangerousGetHandle());
            // outWriter.WriteLine(stderrPipeWriter.DangerousGetHandle());
            // outWriter.WriteLine(stdinPipeReader.DangerousGetHandle());
            outWriter.WriteLine(stdoutPipe.ClientSafePipeHandle.DangerousGetHandle());
            outWriter.WriteLine(stderrPipe.ClientSafePipeHandle.DangerousGetHandle());
            outWriter.WriteLine(stdinPipe.ClientSafePipeHandle.DangerousGetHandle());
            outWriter.WriteLine(appName.Length);
            outWriter.Write(appName);
            outWriter.WriteLine(cmdLine.Length);
            outWriter.Write(cmdLine);

            string procHandleLabel = inReader.ReadLine() ?? "";
            if (procHandleLabel == UacBootstrap.ID_EXCEPTION)
            {
                string errInfo = inReader.ReadLine() ?? "";
                throw new Exception(errInfo);
            }
            Int64 procHandleId = Int64.Parse(inReader.ReadLine() ?? "");
            using SafeNativeHandle procHandle = new SafeNativeHandle((IntPtr)procHandleId, true);

            string threadHandleLabel = inReader.ReadLine() ?? "";
            if (threadHandleLabel == UacBootstrap.ID_EXCEPTION)
            {
                string errInfo = inReader.ReadLine() ?? "";
                throw new Exception(errInfo);
            }
            Int64 threadHandleId = Int64.Parse(inReader.ReadLine() ?? "");
            using SafeNativeHandle threadHandle = new SafeNativeHandle((IntPtr)threadHandleId, true);

            using BlockingCollection<StreamOutput> outputStream = new BlockingCollection<StreamOutput>();
            using EventWaitHandle stdoutDone = new EventWaitHandle(false, EventResetMode.ManualReset);
            using EventWaitHandle stderrDone = new EventWaitHandle(false, EventResetMode.ManualReset);

            PumpProcess(stdoutReader, stdoutDone, stderrReader, stderrDone, threadHandle, procHandle, outputStream);
            stdoutPipe.DisposeLocalCopyOfClientHandle();
            stderrPipe.DisposeLocalCopyOfClientHandle();
            stdinPipe.DisposeLocalCopyOfClientHandle();
            outWriter.WriteLine("");
            // stdinPipe.Dispose();
            // stdoutPipeWriter.Dispose();
            // stderrPipeWriter.Dispose();
            // stdinPipeReader.Dispose();

            foreach (StreamOutput item in outputStream.GetConsumingEnumerable())
            {
                if (item.StreamType == StreamType.StandardOutput)
                {
                    // WriteObject(item.Line);
                }
                else
                {
                    // FIXME: Need to call this internal method.
                    // this.commandRuntime._WriteErrorSkipAllowCheck(record, isFromNativeStdError: true);
                    ErrorRecord stderr = new ErrorRecord(
                        new RemoteException(item.Line),
                        "NativeCommandError",
                        ErrorCategory.NotSpecified,
                        item.Line);
                    WriteError(stderr);
                }
            }

            // stdoutPipeReader.Dispose();
            // stderrPipeReader.Dispose();
            // stdinPipeWriter.Dispose();

            int exitCode = Kernel32.GetExitCodeProcess(procHandle);
            SessionState.PSVariable.Set("lastexitcode", exitCode);
        }

        private static void PumpProcess(StreamReader stdout, EventWaitHandle stdoutDone, StreamReader stderr,
            EventWaitHandle stderrDone, SafeNativeHandle thread, SafeNativeHandle process,
            BlockingCollection<StreamOutput> output)
        {
            ThreadPool.QueueUserWorkItem((s) =>
            {
                // Thread.Sleep(2000);
                while (true)
                {
                    string? line = stdout.ReadLine();
                    if (line == null)
                    {
                        Console.WriteLine("Stdout done");
                        stdoutDone.Set();
                        break;
                    }
                    Console.WriteLine("STDOUT: {0}", line);
                    output.Add(new StreamOutput(line, StreamType.StandardOutput));
                }
            });
            ThreadPool.QueueUserWorkItem((s) =>
            {
                // Thread.Sleep(2000);
                while (true)
                {
                    string? line = stderr.ReadLine();
                    if (line == null)
                    {
                        Console.WriteLine("Stderr done");
                        stderrDone.Set();
                        break;
                    }
                    Console.WriteLine("STDERR: {0}", line);
                    output.Add(new StreamOutput(line, StreamType.StandardError));
                }
            });
            ThreadPool.QueueUserWorkItem((s) =>
            {
                // Console.WriteLine("Waiting for process");
                // Kernel32.WaitForSingleObject(process, 0xFFFFFFFF);
                // Console.WriteLine("Process done");

                stdoutDone.WaitOne();
                Console.WriteLine("Proc Stdout done");
                stderrDone.WaitOne();
                Console.WriteLine("Proc Stderr done");

                Console.WriteLine("Waiting for process");
                Kernel32.WaitForSingleObject(process, 0xFFFFFFFF);
                Console.WriteLine("Process done");

                output.CompleteAdding();
            });

            Kernel32.ResumeThread(thread);
        }
    }

    internal enum StreamType
    {
        StandardOutput,
        StandardError
    }

    internal sealed class StreamOutput
    {
        public string Line { get; }
        public StreamType StreamType { get; }

        public StreamOutput(string line, StreamType streamType)
        {
            Line = line;
            StreamType = streamType;
        }
    }
}
