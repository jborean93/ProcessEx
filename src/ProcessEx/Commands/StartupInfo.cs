using System;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Runtime.InteropServices;

namespace ProcessEx.Commands
{
    [Cmdlet(
        VerbsCommon.Get, "StartupInfo"
    )]
    [OutputType(typeof(StartupInfo))]
    public class GetStartupInfo : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            Native.Helpers.STARTUPINFOW si = Native.Kernel32.GetStartupInfo();
            byte[] reserved2 = Array.Empty<byte>();
            if (si.cbReserved2 > 0)
            {
                reserved2 = new byte[si.cbReserved2];
                Marshal.Copy(si.lpReserved2, reserved2, 0, reserved2.Length);
            }

            WriteObject(new StartupInfo()
            {
                Desktop = Marshal.PtrToStringUni(si.lpDesktop) ?? "",
                Title = Marshal.PtrToStringUni(si.lpTitle) ?? "",
                Position = new Coordinates(si.dwX, si.dwY),
                WindowSize = new Size(si.dwXSize, si.dwYSize),
                CountChars = new Size(si.dwXCountChars, si.dwYCountChars),
                FillAttribute = si.dwFillAttribute,
                Flags = si.dwFlags,
                ShowWindow = si.wShowWindow,
                Reserved = Marshal.PtrToStringUni(si.lpReserved) ?? "",
                Reserved2 = reserved2,
                StandardInput = new Native.SafeNativeHandle(si.hStdInput, false),
                StandardOutput = new Native.SafeNativeHandle(si.hStdOutput, false),
                StandardError = new Native.SafeNativeHandle(si.hStdError, false),
            });
        }
    }

    [Cmdlet(
        VerbsCommon.New, "StartupInfo",
        DefaultParameterSetName = "STDIO"
    )]
    [OutputType(typeof(StartupInfo))]
    public class NewStartupInfo : PSCmdlet
    {
        [Parameter()]
        [ValidateNotNull]
        public string Desktop = "";

        [Parameter()]
        [ValidateNotNull]
        public string Title = "";

        [Parameter()]
        public Coordinates? Position;

        [Parameter()]
        public Size? WindowSize;

        [Parameter()]
        public Size? CountChars;

        [Parameter()]
        public ConsoleFill FillAttribute = ConsoleFill.None;

        [Parameter()]
        public StartupInfoFlags Flags = StartupInfoFlags.None;

        [Parameter()]
        public WindowStyle WindowStyle = WindowStyle.ShowDefault;

        [Parameter()]
        public string Reserved = "";

        [Parameter()]
        public byte[] Reserved2 = Array.Empty<byte>();

        [Parameter(
            ParameterSetName = "STDIO"
        )]
        public SafeHandle? StandardInput;

        [Parameter(
            ParameterSetName = "STDIO"
        )]
        public SafeHandle? StandardOutput;

        [Parameter(
            ParameterSetName = "STDIO"
        )]
        public SafeHandle? StandardError;

        [Parameter(
            ParameterSetName = "ConPTY"
        )]
        public SafeHandle? ConPTY;

        [Parameter()]
        public SafeHandle[] InheritedHandle = Array.Empty<SafeHandle>();

        [Parameter()]
        public SafeHandle[] JobList = Array.Empty<SafeHandle>();

        [Parameter()]
        public ProcessIntString? ParentProcess;

        protected override void ProcessRecord()
        {
            SafeHandle nullHandle = Native.Helpers.NULL_HANDLE_VALUE;

            if (Position != null)
                Flags |= StartupInfoFlags.UsePosition;

            if (WindowSize != null)
                Flags |= StartupInfoFlags.UseSize;

            if (CountChars != null)
                Flags |= StartupInfoFlags.UseCountChars;

            if (FillAttribute != ConsoleFill.None)
                Flags |= StartupInfoFlags.UseFillAttribute;

            if (WindowStyle != WindowStyle.ShowDefault)
                Flags |= StartupInfoFlags.UseShowWindow;

            if (ParameterSetName == "STDIO")
            {
                bool stdinSet = StandardInput != null && StandardInput.DangerousGetHandle() != IntPtr.Zero;
                bool stdoutSet = StandardOutput != null && StandardOutput.DangerousGetHandle() != IntPtr.Zero;
                bool stderrSet = StandardError != null && StandardError.DangerousGetHandle() != IntPtr.Zero;

                if ((Flags & StartupInfoFlags.UseHotKey) == StartupInfoFlags.UseHotKey)
                {
                    if (stdoutSet || stderrSet)
                    {
                        ArgumentException ex = new ArgumentException(
                            "Cannot set StandardOutput or StandardError with the flags UseHotKey");
                        WriteError(new ErrorRecord(ex, "UseHotKeyWithStdio", ErrorCategory.InvalidArgument, null));
                        return;
                    }
                }
                else if (stdinSet || stdoutSet || stderrSet)
                {
                    Flags |= StartupInfoFlags.UseStdHandles;
                }
            }

            WriteObject(new StartupInfo()
            {
                Desktop = Desktop,
                Title = Title,
                Position = Position,
                WindowSize = WindowSize,
                CountChars = CountChars,
                FillAttribute = FillAttribute,
                Flags = Flags,
                ShowWindow = WindowStyle,
                Reserved = Reserved,
                Reserved2 = Reserved2,
                StandardInput = StandardInput ?? nullHandle,
                StandardOutput = StandardOutput ?? nullHandle,
                StandardError = StandardError ?? nullHandle,
                ConPTY = ConPTY ?? nullHandle,
                InheritedHandles = InheritedHandle,
                JobList = JobList,
                ParentProcess = ParentProcess?.ProcessId ?? 0,
                ParentProcessHandle = ParentProcess?.ProcessHandle,
            });
        }
    }
}
