using Microsoft.Win32.SafeHandles;
using ProcessEx.Native;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessEx.Commands;

public enum StdioStreamTarget
{
    /// <summary>
    /// Sends the stream to the error stream as an ErrorRecord.
    /// This is the default for stderr.
    /// </summary>
    Error,
    /// <summary>
    /// Sends the stream to the output stream as a string or byte array.
    /// This is the default for stdout.
    /// </summary>
    Output,
    /// <summary>
    /// Does not redirect the stream and has it write to the console.
    /// </summary>
    Console,
    /// <summary>
    /// Discards all stream output.
    /// </summary>
    Null,
}

public abstract class InvokeProcessBase : PSCmdlet, IDisposable
{
    private sealed class ProcessWaitHandle : WaitHandle
    {
        public ProcessWaitHandle(SafeHandle handle)
        {
            SafeWaitHandle = new(handle.DangerousGetHandle(), false);
        }
    }

    private static PropertyInfo? _errorRecord_PreserveInvocationInfoOnce;

    private readonly CancellationTokenSource _cancelTokenSource = new();
    private readonly BlockingCollection<object> _outData = [];
    private ProcessInfo? _procInfo;

    private SafeFileHandle? _nullHandle;
    internal AnonymousPipeServerStream? _inputStream;
    internal AnonymousPipeServerStream? _outputStream;
    internal AnonymousPipeServerStream? _errorStream;
    internal Task? _outputTask;
    internal Task? _errorTask;

    [Parameter(
        Mandatory = true,
        Position = 0,
        ParameterSetName = "FilePath"
    )]
    public string FilePath { get; set; } = "";

    [Parameter(
        ValueFromRemainingArguments = true,
        ParameterSetName = "FilePath"
    )]
    public string[] ArgumentList { get; set; } = [];

    [Parameter(ParameterSetName = "FilePath")]
    public ArgumentEscapingMode ArgumentEscaping { get; set; } = ArgumentEscapingMode.Standard;

    [Parameter(
        Mandatory = true,
        ParameterSetName = "CommandLine"
    )]
    public string CommandLine { get; set; } = "";

    [Parameter(
        ParameterSetName = "CommandLine"
    )]
    public string ApplicationName { get; set; } = "";

    [Parameter]
    public string WorkingDirectory { get; set; } = "";

    [Parameter]
    public StartupInfo? StartupInfo { get; set; }

    [Parameter]
    public IDictionary? Environment { get; set; }

    [Parameter(ValueFromPipeline = true)]
    public object? InputObject { get; set; }

    [Parameter]
    [EncodingOrByteStreamTransform]
#if NET6_0_OR_GREATER
    [EncodingOrByteStreamCompletions]
#else
    [ArgumentCompleter(typeof(EncodingOrByteStreamCompletionsAttribute))]
#endif
    public EncodingOrByteStream? InputEncoding { get; set; }

    [Parameter]
    [EncodingOrByteStreamTransform]
#if NET6_0_OR_GREATER
    [EncodingOrByteStreamCompletions]
#else
    [ArgumentCompleter(typeof(EncodingOrByteStreamCompletionsAttribute))]
#endif
    public EncodingOrByteStream? OutputEncoding { get; set; }

    [Parameter]
    public StdioStreamTarget RedirectStdout { get; set; } = StdioStreamTarget.Output;

    [Parameter]
    public StdioStreamTarget RedirectStderr { get; set; } = StdioStreamTarget.Error;

    [Parameter]
    public SwitchParameter Raw { get; set; }

    internal bool CloseInputAfterExit { get; set; }

    protected override void BeginProcessing()
    {
        string workingDirectory = string.IsNullOrWhiteSpace(WorkingDirectory)
            ? SessionState.Path.CurrentFileSystemLocation.Path
            : WorkingDirectory;

        if (ParameterSetName == "FilePath")
        {
            ApplicationName = ArgumentHelper.ResolveExecutable(this, FilePath, workingDirectory);

            List<string> commands = [ApplicationName, .. ArgumentList];
            CommandLine = string.Join(" ", commands.Select(a => ArgumentHelper.EscapeArgument(a, ArgumentEscaping)));
        }

        if (InputEncoding?.IsByteStream == true)
        {
            ArgumentException ex = new(
                "Cannot use -InputEncoding Bytes, the input must have an encoding set.");
            ErrorRecord err = new(
                ex,
                "InvokeWithInputEncodingAsBytes",
                ErrorCategory.InvalidArgument,
                null);
            ThrowTerminatingError(err);
        }

        StartupInfo startupInfo = StartupInfo?.Clone() ?? new StartupInfo();
        if (startupInfo.ConPTY.DangerousGetHandle() != IntPtr.Zero)
        {
            ArgumentException ex = new("Cannot use -StartupInfo with a ConPTY handle.");
            ErrorRecord err = new(
                ex,
                "InvokeWithConPTY",
                ErrorCategory.InvalidArgument,
                null);
            ThrowTerminatingError(err);
        }

        if (startupInfo.StandardInput.DangerousGetHandle() != IntPtr.Zero ||
            startupInfo.StandardOutput.DangerousGetHandle() != IntPtr.Zero ||
            startupInfo.StandardError.DangerousGetHandle() != IntPtr.Zero)
        {
            ArgumentException ex = new(
                "Cannot use -StartupInfo with Standard Input, Output, or Error handles.");
            ErrorRecord err = new(
                ex,
                "InvokeWithStdioHandle",
                ErrorCategory.InvalidArgument,
                null);
            ThrowTerminatingError(err);
        }

        SetupProcessIO(startupInfo);

        WriteVerbose(string.Format("Starting new process with\n\t{0}", string.Join("\n\t",
        [
            $"ApplicationName: {ApplicationName}",
            $"CommandLine: {CommandLine}",
            $"WorkingDirectory: {workingDirectory}",
        ])));
        _procInfo = StartProcess(
            ApplicationName,
            CommandLine,
            CreationFlags.None,
            Environment,
            workingDirectory,
            startupInfo);

        _inputStream?.DisposeLocalCopyOfClientHandle();
        _outputStream?.DisposeLocalCopyOfClientHandle();
        _errorStream?.DisposeLocalCopyOfClientHandle();
    }

    protected override void ProcessRecord()
    {
        if (_inputStream is null || InputObject is null)
        {
            return;
        }

        object inData = InputObject;
        if (inData is PSObject psObj)
        {
            inData = psObj.BaseObject;
        }

        Encoding inputEncoding = InputEncoding?.Encoding ?? Console.InputEncoding;
        if (inData is byte byteIn)
        {
            WriteInputBytes(_inputStream, [byteIn]);
        }
        else if (inData is byte[] byteArrayIn)
        {
            WriteInputBytes(_inputStream, byteArrayIn);
        }
        else if (inData is string stringIn)
        {
            WriteInputLine(_inputStream, inputEncoding, stringIn);
        }
        else if (inData is string[] stringArrayIn)
        {
            foreach (string val in stringArrayIn)
            {
                WriteInputLine(_inputStream, inputEncoding, val);
            }
        }
        else if (inData is IList inList)
        {
            foreach (object val in inList)
            {
                if (val is byte byteValue)
                {
                    WriteInputBytes(_inputStream, [byteValue]);
                }
                else
                {
                    string rawString = LanguagePrimitives.ConvertTo<string>(val);
                    WriteInputLine(_inputStream, inputEncoding, rawString);
                }
            }
        }
        else
        {
            string stdinString = LanguagePrimitives.ConvertTo<string>(inData);
            WriteInputLine(_inputStream, inputEncoding, stdinString);
        }

        // See if there is any output already ready to write.
        while (_outData.TryTake(out object? currentOutput, 0, _cancelTokenSource.Token))
        {
            WriteResult(currentOutput);
        }
    }

    protected override void EndProcessing()
    {
        if (_procInfo is null)
        {
            return;
        }

        if (_inputStream is not null && !CloseInputAfterExit)
        {
            WriteVerbose("Closing stdin pipe");
            _inputStream.Close();
        }

        Task<int> procWait = Task.Run(() => WaitForExitAsync(_procInfo, _cancelTokenSource.Token));
        foreach (object data in _outData.GetConsumingEnumerable(_cancelTokenSource.Token))
        {
            WriteResult(data);
        }

        WriteVerbose("Waiting for process to end");
        int rc = procWait.GetAwaiter().GetResult();

        WriteVerbose($"Setting $LASTEXITCODE to {rc}");
        SessionState.PSVariable.Set("global:LASTEXITCODE", rc);
    }

    protected override void StopProcessing()
    {
        if (_procInfo is not null)
        {
            Kernel32.TerminateProcess(_procInfo.Process, -1);
        }
        _cancelTokenSource.Cancel();
    }

    internal abstract ProcessInfo StartProcess(
        string applicationName,
        string commandLine,
        CreationFlags creationFlags,
        IDictionary? environment,
        string workingDirectory,
        StartupInfo startupInfo);

    internal virtual void CloseInputStreams()
    {
        _inputStream?.Dispose();
        _inputStream = null;
    }

    internal virtual void SetupProcessIO(StartupInfo startupInfo)
    {
        if (MyInvocation.ExpectingInput)
        {
            WriteVerbose("Creating stdin pipe");
            _inputStream = new(PipeDirection.Out, HandleInheritability.Inheritable);
            startupInfo.StandardInput = _inputStream.ClientSafePipeHandle;
        }

        startupInfo.StandardOutput = SetupStreamIo("stdout", RedirectStdout);
        startupInfo.StandardError = SetupStreamIo("stderr", RedirectStderr);
    }

    internal SafeHandle SetupStreamIo(string name, StdioStreamTarget target) => target switch
    {
        StdioStreamTarget.Console => SetupConsoleStream(name),
        StdioStreamTarget.Error => SetupErrorStreamAndReader(name),
        StdioStreamTarget.Null => SetupNullStream(name),
        StdioStreamTarget.Output => SetupOutputStreamAndReader(name),
        _ => throw new NotImplementedException(),
    };

    private SafeHandle SetupConsoleStream(string name)
    {
        WriteVerbose($"Setting {name} to use the console.");
        return Helpers.NULL_HANDLE_VALUE;
    }

    private SafeHandle SetupErrorStreamAndReader(string name)
    {
        Encoding encoding = OutputEncoding?.Encoding ?? Console.OutputEncoding;

        WriteVerbose($"Setting {name} to output to the error stream with the encoding {encoding.HeaderName}.");
        _errorStream ??= new(PipeDirection.In, HandleInheritability.Inheritable);
        StreamReader reader = new(_errorStream, encoding, false, 16384, true);
        _errorTask ??= Task.Run(() => ReadStringStream(reader, _outData, Raw, true, _cancelTokenSource.Token));
        return _errorStream.ClientSafePipeHandle;
    }

    private SafeHandle SetupNullStream(string name)
    {
        WriteVerbose($"Setting {name} to output to null.");
        _nullHandle ??= Kernel32.CreateFile(
            // Special Win32 file used so we don't have to manually pump and
            // discard the output.
            "NUL",
            FileSystemRights.Write,
            FileShare.ReadWrite,
            FileMode.Open,
            FileAttributes.Normal,
            Helpers.FileFlags.NONE);
        return _nullHandle;
    }

    private SafeHandle SetupOutputStreamAndReader(string name)
    {
        Encoding? encoding = OutputEncoding?.IsByteStream == true
            ? null
            : OutputEncoding?.Encoding ?? Console.OutputEncoding;

        _outputStream ??= new(PipeDirection.In, HandleInheritability.Inheritable);
        if (encoding is null)
        {
            WriteVerbose($"Setting {name} to output to the output stream as bytes.");
            _outputTask ??= Task.Run(() => ReadByteStream(_outputStream, _outData, Raw, _cancelTokenSource.Token));
        }
        else
        {
            WriteVerbose($"Setting {name} to output to the output stream with the encoding {encoding.HeaderName}.");
            StreamReader reader = new(_outputStream, encoding, false, 16384, true);
            _outputTask ??= Task.Run(() => ReadStringStream(reader, _outData, Raw, false, _cancelTokenSource.Token));
        }

        return _outputStream.ClientSafePipeHandle;
    }

    private static async Task ReadByteStream(
        AnonymousPipeServerStream pipe,
        BlockingCollection<object> outData,
        bool raw,
        CancellationToken cancellationToken)
    {
        int bufferSize = 16 * 1024;

        if (raw)
        {
            using MemoryStream targetStream = new();
            await pipe.CopyToAsync(targetStream, bufferSize, cancellationToken);
            outData.Add(targetStream.ToArray(), cancellationToken);
        }
        else
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                while (true)
                {
#if NET6_0_OR_GREATER
                    int read = await pipe.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken);
#else
                    int read = await pipe.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
#endif
                    if (read == 0)
                    {
                        break;
                    }
                    outData.Add(buffer.AsSpan(0, read).ToArray(), cancellationToken);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    private static async Task ReadStringStream(
        StreamReader reader,
        BlockingCollection<object> outData,
        bool raw,
        bool forError,
        CancellationToken cancellationToken)
    {
        if (raw)
        {
            string value = await reader.ReadToEndAsync();
            if (value == "")
            {
                return;
            }

            if (forError)
            {
                outData.Add(WrapStderrAsError(value), cancellationToken);
            }
            else
            {
                outData.Add(value, cancellationToken);
            }
        }
        else
        {
#if NET6_0_OR_GREATER
            while (true)
            {
                string? line = await reader.ReadLineAsync();
                if (line is null)
                {
                    break;
                }
#else
            while (!reader.EndOfStream)
            {
                // Hangs when reader is closed so we change the loop
                // check for .NET Framework.
                string line = await reader.ReadLineAsync() ?? "";
#endif

                if (forError)
                {
                    outData.Add(WrapStderrAsError(line), cancellationToken);
                }
                else
                {
                    outData.Add(line, cancellationToken);
                }
            }
        }
    }

    private async Task<int> WaitForExitAsync(
        ProcessInfo procInfo,
        CancellationToken cancellationToken)
    {
        ProcessWaitHandle waitHandle = new(procInfo.Process);
        TaskCompletionSource<int> tcs = new();
        using CancellationTokenRegistration cancelRegistration = cancellationToken.Register(
            () => tcs.SetCanceled());

        int rc;
        RegisteredWaitHandle taskWaitHandle = ThreadPool.RegisterWaitForSingleObject(
            waitHandle,
            (s, t) => tcs.SetResult(Kernel32.GetExitCodeProcess(procInfo.Process)),
            null,
            -1,
            true);
        try
        {
            rc = await tcs.Task;
        }
        finally
        {
            _procInfo = null;
            taskWaitHandle.Unregister(waitHandle);
        }

        if (CloseInputAfterExit && _inputStream is not null)
        {
            CloseInputStreams();
        }

        // It is important we wait for the output and error readers to
        // complete before marking the output collection as complete so it
        // stores all the data available.
        if (_outputTask is not null)
        {
            await _outputTask;
        }
        if (_errorTask is not null)
        {
            await _errorTask;
        }
        _outData.CompleteAdding();

        return rc;
    }

    private static ErrorRecord WrapStderrAsError(string stderr)
    {
        _errorRecord_PreserveInvocationInfoOnce ??= typeof(ErrorRecord)
            .GetProperty(
                "PreserveInvocationInfoOnce",
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new RuntimeException(
                "Internal Error: Failed to find ErrorRecord.PreserveInvocationInfoOnce");

        ErrorRecord err = new(
            new RemoteException(stderr),
            "NativeCommandError",
            ErrorCategory.NotSpecified,
            stderr);

        // Needed so that WriteError does not change the FQEID and message to
        // include the current cmdlet. We just want the error as a raw string.
        _errorRecord_PreserveInvocationInfoOnce.SetValue(err, true);

        return err;
    }

    private static void WriteInputLine(
        AnonymousPipeServerStream pipe,
        Encoding inputEncoding,
        string line)
    {
        byte[] data = inputEncoding.GetBytes($"{line}{System.Environment.NewLine}");
        WriteInputBytes(pipe, data);
    }

    private static void WriteInputBytes(
        AnonymousPipeServerStream pipe,
        byte[] data)
    {
        pipe.Write(data, 0, data.Length);
    }

    private void WriteResult(object data)
    {
        if (data is ErrorRecord err)
        {
            WriteError(err);
        }
        else
        {
            // If -Raw we don't want to enumerate any array results.
            WriteObject(data, !Raw);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancelTokenSource.Dispose();
            _inputStream?.Dispose();
            _outputStream?.Dispose();
            _errorStream?.Dispose();
            _nullHandle?.Dispose();
            _procInfo?.Process?.Dispose();
            _procInfo?.Thread?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

[Cmdlet(
    VerbsLifecycle.Invoke, "ProcessEx",
    DefaultParameterSetName = "FilePath"
)]
[Alias("procex")]
[OutputType(typeof(string))]
public sealed class InvokeProcessExCommand : InvokeProcessBase
{
    private SafeConsoleHandle? _conPtyHandle;

    [Parameter]
    public SwitchParameter DisableInheritance { get; set; }

    [Parameter]
    public SafeHandle? Token { get; set; }

    [Parameter]
    public SwitchParameter UseConPTY { get; set; }

    [Parameter]
    [Alias("ConPTYY")]
    public short ConPTYHeight { get; set; } = -1;

    [Parameter]
    [Alias("ConPTYX")]
    public short ConPTYWidth { get; set; } = -1;

    [Parameter]
    public SwitchParameter UseNewEnvironment { get; set; }

    protected override void BeginProcessing()
    {
        if (DisableInheritance && StartupInfo?.InheritedHandles.Length > 0)
        {
            ArgumentException ex = new(
                "Cannot -DisableInheritance with explicit inherited handles in StartupInfo.");
            ErrorRecord err = new(
                ex,
                "DisableInheritedWithInheritedHandles",
                ErrorCategory.InvalidArgument,
                null);
            ThrowTerminatingError(err);
        }

        if (Environment?.Count > 0 && UseNewEnvironment)
        {
            ArgumentException ex = new("Cannot use -Environment with -UseNewEnvironment.");
            ErrorRecord err = new(
                ex,
                "UseNewEnvironmentWithEnvironment",
                ErrorCategory.InvalidArgument,
                null);
            ThrowTerminatingError(err);
        }

#if NET6_0_OR_GREATER
        int currentProcId = System.Environment.ProcessId;
#else
        int currentProcId = System.Diagnostics.Process.GetCurrentProcess().Id;
#endif
        if (
            StartupInfo is not null &&
            StartupInfo.ParentProcess != 0 &&
            StartupInfo.ParentProcess != currentProcId &&
            (
                RedirectStdout == StdioStreamTarget.Console ||
                RedirectStderr == StdioStreamTarget.Console
            )
        )
        {
            ArgumentException ex = new(
                "Invoke-ProcessWith cannot redirect stdout/stderr to the console when using a custom parent process.");
            ErrorRecord err = new(
                ex,
                "InvokeExConsoleRedirectionWithParentProc",
                ErrorCategory.InvalidArgument,
                null);
            ThrowTerminatingError(err);
        }

        base.BeginProcessing();
    }

    internal override void CloseInputStreams()
    {
        base.CloseInputStreams();
        _conPtyHandle?.Dispose();
        _conPtyHandle = null;
    }

    internal override void SetupProcessIO(StartupInfo startupInfo)
    {
        if (UseConPTY)
        {
            // It is important we close the input stream for a ConPTY after the
            // process has ended. Closing it before sends the ctrl+c signal and
            // leaves the conhost process still running causing a hang. The
            // downside is the caller must explicitly exit the process or else
            // it'll continue to wait for input forever.
            CloseInputAfterExit = true;

            // ConPTY always uses UTF-8, set the encoding explicitly except if
            // the output is requested to be bytes.
            if (OutputEncoding?.IsByteStream != true)
            {
                OutputEncoding = new(new UTF8Encoding());
            }
            InputEncoding = new(new UTF8Encoding());

            _inputStream = new(PipeDirection.Out, HandleInheritability.Inheritable);
            SafeHandle outHandle = SetupStreamIo("ConPTY output", StdioStreamTarget.Output);

            short x = ConPTYWidth == -1
                ? (short)(Host?.UI?.RawUI?.BufferSize.Width ?? 80)
                : ConPTYWidth;
            short y = ConPTYHeight == -1
                ? (short)(Host?.UI?.RawUI?.BufferSize.Height ?? 80)
                : ConPTYHeight;
            Helpers.COORD ptySize = new()
            {
                X = x,
                Y = y,
            };

            _conPtyHandle = Kernel32.CreatePseudoConsole(
                ptySize,
                _inputStream.ClientSafePipeHandle,
                outHandle,
                Helpers.PseudoConsoleCreateFlags.NONE);

            startupInfo.ConPTY = _conPtyHandle;

            // Needed to ensure that the existing console handles aren't
            // inherited when using a ConPTY.
            // https://github.com/microsoft/terminal/issues/11276#issuecomment-923210023
            startupInfo.Flags |= StartupInfoFlags.UseStdHandles;
        }
        else
        {
            base.SetupProcessIO(startupInfo);
        }
    }

    internal override ProcessInfo StartProcess(
        string applicationName,
        string commandLine,
        CreationFlags creationFlags,
        IDictionary? environment,
        string workingDirectory,
        StartupInfo startupInfo)
    {
        bool shouldInherit = !DisableInheritance;
        bool isIoRedirected = startupInfo.StandardInput.DangerousGetHandle() != IntPtr.Zero ||
                startupInfo.StandardOutput.DangerousGetHandle() != IntPtr.Zero ||
                startupInfo.StandardError.DangerousGetHandle() != IntPtr.Zero;
        if (DisableInheritance && isIoRedirected)
        {
            // If the caller has asked to disable inheritance but we are
            // redirecting a stream we need to still inherit handles but only
            // out stdio streams as an explicit list.
            startupInfo.InheritStdioHandles = true;
            shouldInherit = true;
        }

#if NET6_0_OR_GREATER
        int currentProcId = System.Environment.ProcessId;
#else
        int currentProcId = System.Diagnostics.Process.GetCurrentProcess().Id;
#endif
        if (
            startupInfo.ParentProcess != 0 &&
            startupInfo.ParentProcess != currentProcId &&
            isIoRedirected
        )
        {
            // If a custom parent process is specified we cannot inherit the
            // existing console so create a new hidden one.
            creationFlags |= CreationFlags.NewConsole;
            startupInfo.ShowWindow = WindowStyle.Hide;
            startupInfo.Flags |= StartupInfoFlags.UseShowWindow;
        }

        if (Token is null)
        {
            return ProcessRunner.CreateProcess(
                applicationName,
                commandLine,
                null,
                null,
                shouldInherit,
                creationFlags,
                environment,
                workingDirectory,
                startupInfo,
                UseNewEnvironment);
        }
        else
        {
            return ProcessRunner.CreateProcessAsUser(
                Token,
                applicationName,
                commandLine,
                null,
                null,
                shouldInherit,
                creationFlags,
                environment,
                workingDirectory,
                startupInfo,
                UseNewEnvironment);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _conPtyHandle?.Dispose();
        }
        base.Dispose(disposing);
    }
}

[Cmdlet(
    VerbsLifecycle.Invoke, "ProcessWith",
    DefaultParameterSetName = "FilePath"
)]
[Alias("procwith")]
[OutputType(typeof(string))]
public sealed class InvokeProcessWithCommand : InvokeProcessBase
{
    [Parameter]
    [Credential]
    public PSCredential Credential { get; set; } = PSCredential.Empty;

    [Parameter]
    public SafeHandle? Token { get; set; }

    [Parameter()]
    public SwitchParameter WithProfile { get; set; }

    [Parameter()]
    public SwitchParameter NetCredentialsOnly { get; set; }

    protected override void BeginProcessing()
    {
        if (Token is null && Credential == PSCredential.Empty)
        {
            Credential = Host.UI.PromptForCredential(
                "cmdlet Invoke-ProcessWith",
                "Supply values for the Credential parameter",
                null,
                null);
        }
        else if (Token is not null && Credential != PSCredential.Empty)
        {
            ParameterBindingException exc = new(
                "Cannot set -Token and -Credential together.");
            ErrorRecord err = new(
                exc,
                "CredentialAndTokenSet",
                ErrorCategory.InvalidArgument,
                null);
            ThrowTerminatingError(err);
        }

        if (RedirectStdout == StdioStreamTarget.Console || RedirectStderr == StdioStreamTarget.Console)
        {
            ArgumentException ex = new("Invoke-ProcessWith cannot redirect stdout/stderr to the console.");
            ErrorRecord err = new(
                ex,
                "InvokeWithConsoleRedirection",
                ErrorCategory.InvalidArgument,
                null);
            ThrowTerminatingError(err);
        }

        base.BeginProcessing();
    }

    internal override ProcessInfo StartProcess(
        string applicationName,
        string commandLine,
        CreationFlags creationFlags,
        IDictionary? environment,
        string workingDirectory,
        StartupInfo startupInfo)
    {
        // The CreateProcessWith* APIs must create a new console and cannot
        // inherit the console. We explicitly hide the console that is created.
        startupInfo.ShowWindow = WindowStyle.Hide;
        startupInfo.Flags |= StartupInfoFlags.UseShowWindow;

        Helpers.LogonFlags logonFlags = Helpers.LogonFlags.NONE;
        if (WithProfile)
        {
            logonFlags |= Helpers.LogonFlags.LOGON_WITH_PROFILE;
        }

        if (NetCredentialsOnly)
        {
            logonFlags |= Helpers.LogonFlags.LOGON_NETCREDENTIALS_ONLY;
        }

        if (Token is null)
        {
            (string username, string? domain) = CredentialHelper.SplitUserName(Credential.UserName);

            return ProcessRunner.CreateProcessWithLogon(
                username,
                domain,
                Credential.Password,
                logonFlags,
                applicationName,
                commandLine,
                creationFlags,
                environment,
                workingDirectory,
                startupInfo);
        }
        else
        {
            return ProcessRunner.CreateProcessWithToken(
                Token,
                logonFlags,
                applicationName,
                commandLine,
                creationFlags,
                environment,
                workingDirectory,
                startupInfo);
        }
    }
}
