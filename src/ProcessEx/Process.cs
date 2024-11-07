using ProcessEx.Native;
using ProcessEx.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Language;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace ProcessEx
{
    public sealed class ProcessIntString
    {
        internal int ProcessId { get; set; }
        internal SafeHandle? ProcessHandle { get; set; }

        public ProcessIntString(int pid) => ProcessId = pid;

        public ProcessIntString(Process process)
        {
            ProcessId = process.Id;

            if (IsAllAccessHandle(process.SafeHandle))
                ProcessHandle = process.SafeHandle;
        }

        public ProcessIntString(ProcessInfo process)
        {
            ProcessId = process.ProcessId;

            if (IsAllAccessHandle(process.Process))
                ProcessHandle = process.Process;
        }

        private bool IsAllAccessHandle(SafeHandle? handle)
        {
            if (handle?.IsInvalid != false && handle?.IsClosed != false)
                return false;
            else if (handle.DangerousGetHandle() == (IntPtr)(-1))
                return true; // Psuedo handle with ALL_ACCESS rights

            using SafeMemoryBuffer buffer = Ntdll.NtQueryObject(handle,
                Helpers.OBJECT_INFORMATION_CLASS.ObjectBasicInformation);
            var bi = Marshal.PtrToStructure<Helpers.PUBLIC_OBJECT_BASIC_INFORMATION>(buffer.DangerousGetHandle());

            return (bi.AccessMask & (uint)ProcessAccessRights.AllAccess) != 0;
        }
    }

    public class StartupInfo : ICloneable
    {
        public string Desktop { get; set; } = "";
        public string Title { get; set; } = "";
        public Coordinates? Position { get; set; }
        public Size? WindowSize { get; set; }
        public Size? CountChars { get; set; }
        public ConsoleFill FillAttribute { get; set; }
        public StartupInfoFlags Flags { get; set; }
        public WindowStyle ShowWindow { get; set; } = WindowStyle.ShowDefault;
        public string Reserved { get; set; } = "";
        public byte[] Reserved2 { get; set; } = [];
        public SafeHandle StandardInput { get; set; } = Helpers.NULL_HANDLE_VALUE;
        public SafeHandle StandardOutput { get; set; } = Helpers.NULL_HANDLE_VALUE;
        public SafeHandle StandardError { get; set; } = Helpers.NULL_HANDLE_VALUE;

        // ProcThreadAttributes
        public SafeHandle ConPTY { get; set; } = Helpers.NULL_HANDLE_VALUE;
        public SafeHandle[] InheritedHandles { get; set; } = [];
        public SafeHandle[] JobList { get; set; } = [];
        public int ParentProcess { get; set; } = 0;
        public ChildProcessPolicy ChildProcessPolicy { get; set; } = ChildProcessPolicy.None;

        public StartupInfo Clone()
            => (StartupInfo)MemberwiseClone();

        object ICloneable.Clone()
            => MemberwiseClone();

        internal SafeHandle? ParentProcessHandle { get; set; }
        internal bool InheritStdioHandles { get; set; }

        internal SafeHandle OpenParentProcessHandle(ProcessAccessRights access)
        {
            return ParentProcessHandle ?? Kernel32.OpenProcess(ParentProcess, access, false);
        }
    }

    [Flags]
    public enum ChildProcessPolicy
    {
        /// <summary>
        /// No child process policy is set.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// PROCESS_CREATION_CHILD_PROCESS_RESTRICTED - The process being
        /// created is not allowed to create child processes.
        /// </summary>
        Restricted = 0x1,
        /// <summary>
        /// PROCESS_CREATION_CHILD_PROCESS_OVERRIDE - The process being created
        /// is allowed to create a child process if it would otherwise be
        /// restricted.
        /// </summary>
        Override = 0x2,
        /// <summary>
        /// PROCESS_CREATION_CHILD_PROCESS_RESTRICTED_UNLESS_SECURE
        /// </summary>
        RestrictedUnlessSecure = 0x4,
    }

    [Flags]
    public enum ConsoleFill : uint
    {
        /// <summary>
        /// No console fill values set.
        /// </summary>
        None = 0x00000000,
        /// <summary>
        /// FOREGROUND_BLUE - Text color contains blue.
        /// </summary>
        ForegroundBlue = 0x00000001,
        /// <summary>
        /// FOREGROUND_GREEN - Text color contains green.
        /// </summary>
        ForegroundGreen = 0x00000002,
        /// <summary>
        /// FOREGROUND_RED - Text color contains red.
        /// </summary>
        ForegroundRed = 0x00000004,
        /// <summary>
        /// FOREGROUND_INTENSITY - Text color is intensified.
        /// </summary>
        ForegroundIntensity = 0x00000008,
        /// <summary>
        /// BACKGROUND_BLUE - Background color contains blue.
        /// </summary>
        BackgroundBlue = 0x00000010,
        /// <summary>
        /// BACKGROUND_GREEN - Background color contains green.
        /// </summary>
        BackgroundGreen = 0x00000020,
        /// <summary>
        /// BACKGROUND_RED - Background color contains red.
        /// </summary>
        BackgroundRed = 0x00000040,
        /// <summary>
        /// BACKGROUND_INTENSITY - Background color is intesified.
        /// </summary>
        BackgroundIntensity = 0x00000080,
    }

    [Flags]
    public enum CreationFlags : uint
    {
        /// <summary>
        /// No flags set.
        /// </summary>
        None = 0x00000000,
        /// <summary>
        /// DEBUG_PROCESS - The calling thread starts and debugs the new process and all child processes created by the
        /// new process.
        /// </summary>
        DebugProcess = 0x00000001,
        /// <summary>
        /// DEBUG_ONLY_THIS_PROCESS - The calling thread starts and debugs the new process. It can receive all related
        /// debug events using WaitForDebugEvent function.
        /// </summary>
        DebugOnlyThisProcess = 0x00000002,
        /// <summary>
        /// CREATE_SUSPENDED - The primary thread of the new process is created in a suspended state, and does not run
        /// until the ResumeThread function is called.
        /// </summary>
        Suspended = 0x00000004,
        /// <summary>
        /// DETACHED_PROCESS - For console processes, the new process does not inherit its parent's console. The new
        /// process can call the AllocConsole function at a later time to create a console. This value cannot be used
        /// with CreateNewConsole.
        /// </summary>
        DetachedProcess = 0x00000008,
        /// <summary>
        /// CREATE_NEW_CONSOLE - The new process has a new console, instead of inheriting its parent's console. This
        /// flag cannot be used with DetachedProcess.
        /// </summary>
        NewConsole = 0x00000010,
        /// <summary>
        /// NORMAL_PRIORITY_CLASS - Process has no special scheduling needs.
        /// </summary>
        NormalPriorityClass = 0x00000020,
        /// <summary>
        /// IDLE_PRIORITY_CLASS - Process whose threads run only when the system is idle.
        /// </summary>
        IdlePriorityClass = 0x00000040,
        /// <summary>
        /// HIGH_PRIORITY_CLASS - Process that performs time-critical tasks that must be executed immediately.
        /// </summary>
        HighPriorityClass = 0x00000080,
        /// <summary>
        /// REALTIME_PRIORITY_CLASS - Process that has the highest possible priority.
        /// </summary>
        RealtimePriorityClass = 0x00000100,
        /// <summary>
        /// CREATE_NEW_PROCESS_GROUP - The new process is the root process of a new process group. The process group
        /// includes all processes that are descendants of this root process. The process identifier of the new
        /// process group is the same as the process identifier, which is returned. Process groups are used by the
        /// GenerateConsoleCtrlEvent function to enable sending a CTRL + BREAK signal to a group of console processes.
        /// If this flag is specified, CTRL + C signals will be disabled for all processes within the new process
        /// group. THis flag is ignored if specified with NewConsole.
        /// </summary>
        NewProcessGroup = 0x00000200,
        /// <summary>
        /// CREATE_UNICODE_ENVIRONMENT - If this flag is set, the environment block pointed to uses Unicode characters.
        /// Otherwise the environment block uses ANSI characters.
        /// </summary>
        UnicodeEnvironment = 0x00000400,
        /// <summary>
        /// CREATE_SEPARATE_WOW_VDM - This flag is valid only when starting a 16-bit Windows-based application. If set,
        /// the new process runs in a private Virtual DOS Machine (VDM).
        /// </summary>
        SeparateWowVDM = 0x00000800,
        /// <summary>
        /// CREATE_SHARED_WOW_VDM - The flag is valid only when starting a 16-bit Windows-based application. If the
        /// DefaultSeparateVDM switch in the Windows section of WIN.INI is TRUE, this flag overrides the switch. The
        /// new process is run in the shared Virtual DOS Machine.
        /// </summary>
        SharedWowVDM = 0x00001000,
        /// <summary>
        /// CREATE_FORCEDOS - No longer used.
        /// </summary>
        ForceDOS = 0x00002000,
        /// <summary>
        /// BELOW_NORMAL_PRIORITY_CLASS - Process that has priority above IdlePriorityClass but below
        /// NormalPriorityClass.
        /// </summary>
        BelowNormalPriorityClass = 0x00004000,
        /// <summary>
        /// ABOVE_NORMAL_PRIORITY_CLASS - Process that ahs priority above NormalPriorityClass but below
        /// HighPriorityClass.
        /// </summary>
        AboveNormalPriorityClass = 0x00008000,
        /// <summary>
        /// INHERIT_PARENT_AFFINITY - The process inherits its parent's affinity. if the parent process has threads in
        /// more than one processor group, the new process inherits the group-relative affinity of an arbitrary group
        /// in use by the parent.
        /// </summary>
        InheritParentAffinity = 0x00010000,
        /// <summary>
        /// INHERIT_CALLER_PRIORITY - Inherits the priority of the caller process.
        /// </summary>
        InheritCallerPriority = 0x00020000,
        /// <summary>
        /// CREATE_PROTECTED_PROCESS - The process is to be run as a protected process. The system restricts access to
        /// protected processes and the threads of protected processes. To activate a protected process, the binary
        /// must have a special signature provided by Microsoft.
        /// </summary>
        ProtectedProcess = 0x00040000,
        /// <summary>
        /// EXTENDED_STARTUPINFO_PRESENT - The process is created with extended startup information. This is set
        /// automatically by the Start-ProcessEx cmdlets.
        /// </summary>
        ExtendedStartupinfoPresent = 0x00080000,
        /// <summary>
        /// PROCESS_MODE_BACKGROUND_BEGIN - Begin background processing mode. The system lowers the resource
        /// scheduling priorities of the process so that it can perform background work without significantly affecting
        /// activity in the foreground.
        /// </summary>
        ProcessModeBackgroundBegin = 0x00100000,
        /// <summary>
        /// PROCESS_MODE_BACKGROUND_END - End background processing mode. The system restores the resource scheduling
        /// priorities of the process as they were before the process entered background processing mode.
        /// </summary>
        ProcessModeBackgroundEnd = 0x00200000,
        /// <summary>
        /// CREATE_SECURE_PROCESS - This flag allows secure processes, that run in the Virtualization-Based Security
        /// environment, to launch.
        /// </summary>
        SecureProcess = 0x00400000,
        /// <summary>
        /// CREATE_BREAKAWAY_FROM_JOB - The child process associated with a job are not associated with the job. If
        /// the calling process is not associated with a job, this has no effect. If the calling process is associated
        /// with a job, the job must set JOB_OBJECT_LIMIT_BREAKAWAY_OK.
        /// </summary>
        BreakawayFromJob = 0x01000000,
        /// <summary>
        /// CREATE_PRESERVE_CODE_AUTHZ_LEVEL - Allows the caller to execute a child process that bypasses the process
        /// restrictions that would normally be applied automatically to the process.
        /// </summary>
        PreserveCodeAuthzLevel = 0x02000000,
        /// <summary>
        /// CREATE_DEFAULT_ERROR_MODE - The new process does not inherit the error mode of the calling process.
        /// Instead, the new process gets the default error mode. This feature is particularly useful for multithreaded
        /// shell applications that run with hard errors disabled. The default behavior is for the new process to
        /// inherit the error mode of the caller. Setting this flag changes that default behavior.
        /// </summary>
        DefaultErrorMode = 0x04000000,
        /// <summary>
        /// CREATE_NO_WINDOW - The process is a console application that is being run without a console window.
        /// Therefore, the console handle for the application is not set. This flag is ignored if the application is
        /// not a console application, or if it used with either NewConsole or DetachedProcess.
        /// </summary>
        NoWindow = 0x08000000,
        /// <summary>
        /// PROFILE_USER - Unknown
        /// </summary>
        ProfileUser = 0x10000000,
        /// <summary>
        /// PROFILE_KERNEL - Unknown
        /// </summary>
        ProfileKernel = 0x20000000,
        /// <summary>
        /// PROFILE_SERVER - Unknown
        /// </summary>
        ProfileServer = 0x40000000,
        /// <summary>
        /// CREATE_IGNORE_SYSTEM_DEFAULT - Unknown
        /// </summary>
        IgnoreSystemDefault = 0x80000000,
    }

    public enum WindowStyle : ushort
    {
        /// <summary>
        /// SW_HIDE - Hides the window and activates another window.
        /// </summary>
        Hide = 0x0000,
        /// <summary>
        /// SW_SHOWNORMAL - Activates and displays a window.
        /// </summary>
        ShowNormal = 0x0001,
        /// <summary>
        /// SW_NORMAL - Same as ShowNormal.
        /// </summary>
        Normal = ShowNormal,
        /// <summary>
        /// SW_SHOWMINIMIZED - Activates the window and displays it as a minimized window.
        /// </summary>
        ShowMinimized = 0x0002,
        /// <summary>
        /// SW_SHOWMAXIMIZED - Actiavtes the window and displays it as a maximized window.
        /// </summary>
        ShowMaximized = 0x0003,
        /// <summary>
        /// SW_MAXIMIZE - Same as ShowMaximized.
        /// </summary>
        Maximize = ShowMaximized,
        /// <summary>
        /// SW_SHOWNOACTIVATE - Displays a window in it smost recent size and position without activating it.
        /// </summary>
        ShowNoActivate = 0x0004,
        /// <summary>
        /// SW_SHOW - Activates the window and diplays it in its current size and position.
        /// </summary>
        Show = 0x0005,
        /// <summary>
        /// SW_MINIMIZE - Minimizes the specific window and activates the next top-level window in the Z order.
        /// </summary>
        Minimize = 0x0006,
        /// <summary>
        /// SW_SHOWMINNOACTIVE - Displays the window as a minimized window without activating it.
        /// </summary>
        ShowMinNoActive = 0x0007,
        /// <summary>
        /// SW_SHOWNA - Displays tyhe window in its current size and position without activating it.
        /// </summary>
        ShowNA = 0x0008,
        /// <summary>
        /// SW_RESTORE - Activates and displays the window even if its minimized or maximized.
        /// </summary>
        Restore = 0x0009,
        /// <summary>
        /// SW_SHOWDEFAULT - No window style is set in the STARTUPINFO.
        /// </summary>
        ShowDefault = 0x0010,
        /// <summary>
        /// SW_FORCEMINIMIZE - Minimizes a windows, even if the thread that owns the window is not responding.
        /// </summary>
        ForceMinimize = 0x0011,
    }

    [Flags]
    public enum StartupInfoFlags : uint
    {
        /// <summary>
        /// No flags are set in the STARTUPINFO.
        /// </summary>
        None = 0x00000000,
        /// <summary>
        /// STARTF_USESHOWWINDOW - The ShowWindow value is set.
        /// </summary>
        UseShowWindow = 0x00000001,
        /// <summary>
        /// STARTF_USESIZE - The WindowSize value is set.
        /// </summary>
        UseSize = 0x00000002,
        /// <summary>
        /// STARTF_USEPOSITION - The Position value is set.
        /// </summary>
        UsePosition = 0x00000004,
        /// <summary>
        /// STARTF_USECOUNTCHARS - The CountChars value is set.
        /// </summary>
        UseCountChars = 0x00000008,
        /// <summary>
        /// STARTF_USEFILLATTRIBUTE - The FillAttribute value is set.
        /// </summary>
        UseFillAttribute = 0x00000010,
        /// <summary>
        /// STARTF_RUNFULLSCREEN - Process should be run in full-screen mode.
        /// </summary>
        RunFullscreen = 0x00000020,
        /// <summary>
        /// STARTF_FORCEONFEEDBACK - Cursor is set in feeback mode after the process is created.
        /// </summary>
        ForceOnFeedback = 0x00000040,
        /// <summary>
        /// STARTF_FORCEOFFFEEDBACK - Cursor is set in normal mode after the process is created.
        /// </summary>
        ForceOffFeedback = 0x00000080,
        /// <summary>
        /// STARTF_USESTDHANDLES - The Standard IO handles are set with a value. This cannot be used with UseHotKey.
        /// </summary>
        UseStdHandles = 0x00000100,
        /// <summary>
        /// STARTF_USEHOTKEY - The StandardInput has been set with a value. This cannot be used with
        /// UseStdHandles.
        /// </summary>
        UseHotKey = 0x00000200,
        /// <summary>
        /// STARTF_TITLEISLINKNAME - The Title contains the path of the shortcut file used to start this process.
        /// This cannot be used with TitleIsAppID.
        /// </summary>
        TitleIsLinkName = 0x00000800,
        /// <summary>
        /// STARTF_TITLEISAPPID - The Title contains the AppUserModeID which controls how the taskbar and startmenu
        /// present the application. This cannot be used with TitleIsLinkName.
        /// </summary>
        TitleIsAppID = 0x00001000,
        /// <summary>
        /// STARTF_PREVENTPINNING - Indicates that any window created by the process cannot be pinned on the taskbar.
        /// This must be used with TitleIsAppID.
        /// </summary>
        PreventPinning = 0x00002000,
        /// <summary>
        /// STARTF_UNTRUSTEDSOURCE - The command line came from an untrusted source.
        /// </summary>
        UntrustedSource = 0x00008000,
    }

    internal class ProcessCompletor : IArgumentCompleter
    {
        private readonly ProcessInfo _currentProc = ProcessInfo.GetCurrentProcess();

        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName,
            string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            if (String.IsNullOrWhiteSpace(wordToComplete))
                wordToComplete = "";

            // Favour the current process first.
            if (_currentProc.ProcessId.ToString().StartsWith(wordToComplete))
                yield return ProcessResult(_currentProc, "Current Process");

            foreach (int pid in Kernel32.EnumProcesses())
            {
                if (pid == _currentProc.ProcessId || !pid.ToString().StartsWith(wordToComplete))
                    continue;

                SafeNativeHandle handle;
                ProcessAccessRights access = ProcessAccessRights.QueryInformation |
                    ProcessAccessRights.VMRead;

                if (commandName == "Copy-HandleToProcess")
                {
                    access |= ProcessAccessRights.DupHandle;
                }
                else if (commandName == "Get-ProcessEx")
                {
                    string accessString = fakeBoundParameters.Contains("Access") ?
                        (string)(fakeBoundParameters["Access"] ?? "") : "";

                    // The default value is AllAccess so use that if it cannot parse the -Access value.
                    if (Enum.TryParse<ProcessAccessRights>(accessString, true, out var desiredAccess))
                        access |= desiredAccess;
                    else
                        access = ProcessAccessRights.AllAccess;
                }
                else if (commandName == "New-StartupInfo")
                {
                    access |= ProcessAccessRights.CreateProcess;
                }

                try
                {
                    handle = Kernel32.OpenProcess(pid, access, false);
                }
                catch (NativeException)
                {
                    // If we couldn't open the handle then we assume it's not viable for the cmdlet and thus don't
                    // add to the completion results.
                    continue;
                }

                using (handle)
                {
                    // We don't use the thread at all here so just leave it blank.
                    ProcessInfo proc = new ProcessInfo(handle, Helpers.NULL_HANDLE_VALUE, pid: pid);
                    yield return ProcessResult(proc);
                }
            }
        }

        private CompletionResult ProcessResult(ProcessInfo process, string info = "")
        {
            string cmdLine;
            try
            {
                cmdLine = process.CommandLine;
            }
            catch (NativeException e)
            {
                cmdLine = $"Unknown CommandLine: {e.Message}";
            }

            if (String.IsNullOrEmpty(info))
            {
                try
                {
                    info = process.Executable;
                }
                catch (NativeException e)
                {
                    info = $"Unknown Process: {e.Message}";
                }
            }

            int pid = process.ProcessId;
            return new CompletionResult(pid.ToString(), $"{pid}: {info}", CompletionResultType.ParameterValue,
                cmdLine);
        }
    }

    public static class ProcessRunner
    {
        public static void ResumeThread(SafeHandle thread)
        {
            Kernel32.ResumeThread(thread);
        }

        private delegate ProcessInfo CreateProcessDelegate(string? applicationName, string? commandLine,
            SafeHandle processAttributes, SafeHandle threadAttributes, bool inheritHandles,
            CreationFlags creationflags, SafeHandle environment, string? currentDirectory,
            Helpers.STARTUPINFOEX startupInfo, Dictionary<string, object?> ext);

        internal static ProcessInfo CreateProcess(string? applicationName, string? commandLine,
            SecurityAttributes? processAttributes, SecurityAttributes? threadAttributes, bool inheritHandles,
            CreationFlags creationFlags, IDictionary? environment, string? currentDirectory, StartupInfo startupInfo,
            bool newEnvironment)
        {
            using SafeHandle userToken = Advapi32.OpenProcessToken(Kernel32.GetCurrentProcess(),
                TokenAccessLevels.Query);

            return CreateProcessInternal(applicationName, commandLine, processAttributes, threadAttributes,
                inheritHandles, creationFlags, environment, currentDirectory, startupInfo, newEnvironment,
                userToken, null, CreateProcessDel);
        }

        private static ProcessInfo CreateProcessDel(string? applicationName, string? commandLine,
            SafeHandle processAttributes, SafeHandle threadAttributes, bool inheritHandles,
            CreationFlags creationflags, SafeHandle environment, string? currentDirectory,
            Helpers.STARTUPINFOEX startupInfo, Dictionary<string, object?> _)
        {
            return Kernel32.CreateProcess(applicationName, commandLine, processAttributes, threadAttributes,
                inheritHandles, creationflags, environment, currentDirectory, startupInfo);
        }

        internal static ProcessInfo CreateProcessAsUser(SafeHandle token, string? applicationName, string? commandLine,
            SecurityAttributes? processAttributes, SecurityAttributes? threadAttributes, bool inheritHandles,
            CreationFlags creationFlags, IDictionary? environment, string? currentDirectory, StartupInfo startupInfo,
            bool newEnvironment)
        {
            Dictionary<string, object?> ext = new Dictionary<string, object?>()
            {
                { "token", token },
            };
            return CreateProcessInternal(applicationName, commandLine, processAttributes, threadAttributes,
                inheritHandles, creationFlags, environment, currentDirectory, startupInfo, newEnvironment, token,
                ext, CreateProcessAsUserDel);
        }

        private static ProcessInfo CreateProcessAsUserDel(string? applicationName, string? commandLine,
            SafeHandle processAttributes, SafeHandle threadAttributes, bool inheritHandles,
            CreationFlags creationflags, SafeHandle environment, string? currentDirectory,
            Helpers.STARTUPINFOEX startupInfo, Dictionary<string, object?> ext)
        {
            SafeHandle token = (SafeHandle)ext["token"]!;

            SecurityIdentifier sidToAdd = GetSIDForStation(token);
            GrantStationDesktopAccess(sidToAdd, startupInfo.startupInfo.lpDesktop);

            return Advapi32.CreateProcessAsUser(token, applicationName, commandLine, processAttributes,
                threadAttributes, inheritHandles, creationflags, environment, currentDirectory, startupInfo);
        }

        internal static ProcessInfo CreateProcessWithLogon(string username, string? domain, SecureString password,
            Helpers.LogonFlags logonFlags, string? applicationName, string? commandLine, CreationFlags creationFlags,
            IDictionary? environment, string? currentDirectory, StartupInfo startupInfo)
        {
            Dictionary<string, object?> ext = new Dictionary<string, object?>()
            {
                { "username", username },
                { "domain", domain },
                { "password", password },
                { "logonFlags", logonFlags },
            };

            // If not explicit desktop/station was specified and the current process is running in session 0, the new
            // process will fail with Access Denied. By setting an explicit station/desktop in this field for these
            // scenarios the process will be able to spawn in the current station/desktop. The rules for how a process
            // decides on a desktop/station is in but in the session 0 (non-interactive) case it will try to create a
            // new station based on the logon id (not known right now) but fails due to access permissions. By
            // explicitly setting the desktop/station we can get it to use the current one.
            // https://learn.microsoft.com/en-us/windows/win32/winstation/process-connection-to-a-window-station
            if (string.IsNullOrWhiteSpace(startupInfo.Desktop))
            {
                using SafeNativeHandle currentToken = Advapi32.OpenProcessToken(Kernel32.GetCurrentProcess(),
                    TokenAccessLevels.Query);
                UInt32 currentSessionId = GetTokenSessionid(currentToken);

                if (currentSessionId == 0)
                {
                    using SafeHandle currentStation = User32.GetProcessWindowStation();
                    using SafeHandle currentDesktop = User32.GetThreadDesktop(Kernel32.GetCurrentThreadId());
                    startupInfo.Desktop = string.Format("{0}\\{1}",
                        GetObjectName(currentStation), GetObjectName(currentDesktop));
                }
            }
            return CreateProcessInternal(applicationName, commandLine, null, null, false, creationFlags, environment,
                currentDirectory, startupInfo, false, null, ext, CreateProcessWithLogonDel);
        }

        private static ProcessInfo CreateProcessWithLogonDel(string? applicationName, string? commandLine,
            SafeHandle _1, SafeHandle _2, bool _3, CreationFlags creationflags, SafeHandle environment,
            string? currentDirectory, Helpers.STARTUPINFOEX startupInfo, Dictionary<string, object?> ext)
        {
            // Fails with the extended startup info.
            creationflags &= ~CreationFlags.ExtendedStartupinfoPresent;
            if (startupInfo.lpAttributeList != IntPtr.Zero)
                throw new ArgumentException("CreateProcessWithLogon does not support extended startup information");

            string username = (string)ext["username"]!;
            string? domain = (string?)ext["domain"];
            SecureString password = (SecureString)ext["password"]!;
            Helpers.LogonFlags logonFlags = (Helpers.LogonFlags)ext["logonFlags"]!;

            // When no Desktop is specified Windows seems to set the Logon Session ID to the current process when it
            // logs on the specified user allowing it to access the inherited station/desktop so we don't have to
            // change the DACL for that. When explicitly set we still need to add our user to the specified
            // station/desktop.
            if (startupInfo.startupInfo.lpDesktop != IntPtr.Zero)
            {
                // We don't know the logon session SID as the function will create that so just use the account SID
                // on the new Station/Desktop ACE.
                NTAccount account = String.IsNullOrWhiteSpace(domain)
                    ? new NTAccount(username) : new NTAccount(domain, username);
                SecurityIdentifier targetAccount = (SecurityIdentifier)account.Translate(
                    typeof(SecurityIdentifier));

                GrantStationDesktopAccess(targetAccount, startupInfo.startupInfo.lpDesktop);
            }

            IntPtr passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(password);
            try
            {
                return Advapi32.CreateProcessWithLogon(username, domain, new SafeNativeHandle(passwordPtr, false),
                    logonFlags, applicationName, commandLine, creationflags, environment, currentDirectory,
                    startupInfo);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);
            }
        }

        internal static ProcessInfo CreateProcessWithToken(SafeHandle token, Helpers.LogonFlags logonFlags,
            string? applicationName, string? commandLine, CreationFlags creationFlags, IDictionary? environment,
            string? currentDirectory, StartupInfo startupInfo)
        {
            Dictionary<string, object?> ext = new Dictionary<string, object?>()
            {
                { "token", token },
                { "logonFlags", logonFlags },
            };

            return CreateProcessInternal(applicationName, commandLine, null, null, false, creationFlags,
                environment, currentDirectory, startupInfo, false, null, ext, CreateProcessWithTokenDel);
        }

        private static ProcessInfo CreateProcessWithTokenDel(string? applicationName, string? commandLine,
            SafeHandle _1, SafeHandle _2, bool _3, CreationFlags creationflags, SafeHandle environment,
            string? currentDirectory, Helpers.STARTUPINFOEX startupInfo, Dictionary<string, object?> ext)
        {
            // Fails with the extended startup info.
            creationflags &= ~CreationFlags.ExtendedStartupinfoPresent;
            if (startupInfo.lpAttributeList != IntPtr.Zero)
                throw new ArgumentException("CreateProcessWithToken does not support extended startup information");

            SafeHandle token = (SafeHandle)ext["token"]!;
            Helpers.LogonFlags logonFlags = (Helpers.LogonFlags)ext["logonFlags"]!;

            SecurityIdentifier sidToAdd = GetSIDForStation(token);
            GrantStationDesktopAccess(sidToAdd, startupInfo.startupInfo.lpDesktop);

            return Advapi32.CreateProcessWithToken(token, logonFlags, applicationName, commandLine,
                creationflags, environment, currentDirectory, startupInfo);
        }

        private static ProcessInfo CreateProcessInternal(string? applicationName, string? commandLine,
            SecurityAttributes? processAttributes, SecurityAttributes? threadAttributes,
            bool inheritHandles, CreationFlags creationFlags, IDictionary? environment,
            string? currentDirectory, StartupInfo startupInfo, bool newEnvironment, SafeHandle? userToken,
            Dictionary<string, object?>? ext, CreateProcessDelegate impl)
        {
            if (String.IsNullOrWhiteSpace(applicationName))
                applicationName = null;

            if (String.IsNullOrWhiteSpace(currentDirectory))
                currentDirectory = null;

            creationFlags |= CreationFlags.ExtendedStartupinfoPresent;
            Helpers.STARTUPINFOEX si = new Helpers.STARTUPINFOEX();
            si.startupInfo.cb = (UInt32)Marshal.SizeOf(si);
            si.startupInfo.dwFlags = startupInfo.Flags;

            if (startupInfo.Position != null)
            {
                si.startupInfo.dwX = ((Coordinates)startupInfo.Position).X;
                si.startupInfo.dwY = ((Coordinates)startupInfo.Position).Y;
                si.startupInfo.dwFlags |= StartupInfoFlags.UsePosition;
            }

            if (startupInfo.WindowSize != null)
            {
                si.startupInfo.dwXSize = ((Size)startupInfo.WindowSize).Width;
                si.startupInfo.dwYSize = ((Size)startupInfo.WindowSize).Height;
                si.startupInfo.dwFlags |= StartupInfoFlags.UseSize;
            }

            if (startupInfo.CountChars != null)
            {
                si.startupInfo.dwXCountChars = ((Size)startupInfo.CountChars).Width;
                si.startupInfo.dwYCountChars = ((Size)startupInfo.CountChars).Height;
                si.startupInfo.dwFlags |= StartupInfoFlags.UseCountChars;
            }

            if (startupInfo.FillAttribute != 0)
            {
                si.startupInfo.dwFillAttribute = startupInfo.FillAttribute;
                si.startupInfo.dwFlags |= StartupInfoFlags.UseFillAttribute;
            }

            if (startupInfo.ShowWindow != WindowStyle.ShowDefault)
            {
                si.startupInfo.wShowWindow = startupInfo.ShowWindow;
                si.startupInfo.dwFlags |= StartupInfoFlags.UseShowWindow;
            }

            using var lpReserved = CreateStringBuffer(startupInfo.Reserved);
            using var desktop = CreateStringBuffer(startupInfo.Desktop);
            using var title = CreateStringBuffer(startupInfo.Title);
            using var lpReserved2 = CreateMemoryBuffer(startupInfo.Reserved2);
            using var stdin = PrepareStdioHandle(startupInfo.StandardInput, startupInfo);
            using var stdout = PrepareStdioHandle(startupInfo.StandardOutput, startupInfo);
            using var stderr = PrepareStdioHandle(startupInfo.StandardError, startupInfo);
            using var procThreadAttr = CreateProcThreadAttributes(startupInfo, stdin, stdout, stderr);
            using var processAttr = CreateSecurityAttributes(processAttributes);
            using var threadAttr = CreateSecurityAttributes(threadAttributes);
            using var lpEnvironment = CreateEnvironmentPointer(environment, newEnvironment, userToken);

            si.startupInfo.lpReserved = lpReserved.DangerousGetHandle();
            si.startupInfo.lpDesktop = desktop.DangerousGetHandle();
            si.startupInfo.lpTitle = title.DangerousGetHandle();

            if (startupInfo.Reserved2 != null)
                si.startupInfo.cbReserved2 = (UInt16)startupInfo.Reserved2.Length;

            si.startupInfo.lpReserved2 = lpReserved2.DangerousGetHandle();
            si.startupInfo.hStdInput = stdin.DangerousGetHandle();
            si.startupInfo.hStdOutput = stdout.DangerousGetHandle();
            si.startupInfo.hStdError = stderr.DangerousGetHandle();

            if (
                (startupInfo.Flags & StartupInfoFlags.UseHotKey) == 0 &&
                (
                    si.startupInfo.hStdInput != IntPtr.Zero ||
                    si.startupInfo.hStdOutput != IntPtr.Zero ||
                    si.startupInfo.hStdError != IntPtr.Zero
                )
            )
            {
                si.startupInfo.dwFlags |= StartupInfoFlags.UseStdHandles;
            }

            si.lpAttributeList = procThreadAttr.DangerousGetHandle();

            if (lpEnvironment.DangerousGetHandle() != IntPtr.Zero)
                creationFlags |= CreationFlags.UnicodeEnvironment;

            return impl(applicationName, commandLine, processAttr, threadAttr, inheritHandles,
                creationFlags, lpEnvironment, currentDirectory, si, ext ?? new Dictionary<string, object?>());
        }

        internal static void ResumeAndWait(ProcessInfo processInfo)
        {
            // Thanks to Raymond for these details https://devblogs.microsoft.com/oldnewthing/20130405-00/?p=4743
            int compPortSize = Marshal.SizeOf(typeof(Helpers.JOBOBJECT_ASSOCIATE_COMPLETION_PORT));

            using SafeHandle job = Kernel32.CreateJobObject(null, Helpers.NULL_HANDLE_VALUE);
            using SafeHandle ioPort = Kernel32.CreateIoCompletionPort(Helpers.INVALID_HANDLE_VALUE,
                Helpers.NULL_HANDLE_VALUE, UIntPtr.Zero, 1);
            using SafeMemoryBuffer compPortPtr = new SafeMemoryBuffer(compPortSize);

            var compPort = new Helpers.JOBOBJECT_ASSOCIATE_COMPLETION_PORT()
            {
                CompletionKey = job.DangerousGetHandle(),
                CompletionPort = ioPort.DangerousGetHandle(),
            };
            Marshal.StructureToPtr(compPort, compPortPtr.DangerousGetHandle(), false);

            Kernel32.SetInformationJobObject(job,
                Helpers.JobObjectInformationClass.JobObjectAssociateCompletionPortInformation, compPortPtr,
                compPortSize);
            Kernel32.AssignProcessToJobObject(job, processInfo.Process);

            // Resume the thread and wait until it has exited.
            Kernel32.ResumeThread(processInfo.Thread);
            Kernel32.WaitForSingleObject(processInfo.Process, 0xFFFFFFFF);

            // Continue to poll the job until it receives JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO (4) that indicates
            // all other processes in that job have finished.
            UInt32 completionCode = 0;
            do
            {
                Kernel32.GetQueuedCompletionStatus(ioPort, 0xFFFFFFFF, out completionCode, out var completionKey,
                    out var overlapped);
            } while (completionCode != 4);
        }

        internal static Dictionary<string, string> ConvertEnvironmentBlock(SafeHandle block)
        {
            // Env vars are case insensitive on Windows.
            var comparer = StringComparer.OrdinalIgnoreCase;
            Dictionary<string, string> env = new Dictionary<string, string>(comparer);

            IntPtr ptr = block.DangerousGetHandle();
            while (true)
            {
                string? entry = Marshal.PtrToStringUni(ptr);
                if (String.IsNullOrEmpty(entry))
                    break;

                ptr = IntPtr.Add(ptr, (entry.Length * 2) + 2);
                // Windows stores the drive scoped working directory under the env var =<drive>:=<path>, these aren't
                // actual env vars so they are skipped.
                if (entry.StartsWith("="))
                    continue; // Unsure why this appears but it isn't valid

                string[] valueSplit = entry.Split(new char[1] { '=' }, 2);
                env[valueSplit[0]] = valueSplit[1];
            }

            return env;
        }

        private static SafeHandle CreateEnvironmentPointer(IDictionary? environment, bool newEnvironment,
            SafeHandle? userToken)
        {
            if (newEnvironment)
            {
                if (userToken == null)
                    throw new ArgumentNullException("Cannot create new environment without user token");

                return Userenv.CreateEnvironmentBlock(userToken, false);
            }

            if (environment?.Count > 0)
            {
                StringBuilder environmentString = new StringBuilder();
                foreach (DictionaryEntry? kv in environment)
                    environmentString.AppendFormat("{0}={1}\0", kv?.Key, kv?.Value);
                environmentString.Append('\0');

                IntPtr lpEnvironment = Marshal.StringToHGlobalUni(environmentString.ToString());
                return new SafeMemoryBuffer(lpEnvironment, 0);
            }
            else
            {
                return Helpers.NULL_HANDLE_VALUE;
            }
        }

        private static SafeHandle CreateMemoryBuffer(byte[] data)
        {
            if (data == null || data.Length < 1)
                return Helpers.NULL_HANDLE_VALUE;

            SafeMemoryBuffer buffer = new SafeMemoryBuffer(data.Length);
            Marshal.Copy(data, 0, buffer.DangerousGetHandle(), data.Length);

            return buffer;
        }

        private static SafeHandle CreateProcThreadAttributes(StartupInfo startupInfo, SafeHandle stdin,
            SafeHandle stdout, SafeHandle stderr)
        {
            int count = 0;
            if (startupInfo.ParentProcess != 0)
                count++;

            if (startupInfo.InheritedHandles.Length > 0 || startupInfo.InheritStdioHandles)
                count++;

            if (startupInfo.JobList.Length > 0)
                count++;

            if (startupInfo.ConPTY.DangerousGetHandle() != IntPtr.Zero)
                count++;

            if (startupInfo.ChildProcessPolicy != ChildProcessPolicy.None)
            {
                count++;
            }

            if (count == 0)
                return Helpers.NULL_HANDLE_VALUE;

            SafeProcThreadAttribute attr = Kernel32.InitializeProcThreadAttributeList(count);
            try
            {
                if (startupInfo.ParentProcess != 0)
                {
                    SafeNativeHandle parentProcess = Kernel32.OpenProcess(startupInfo.ParentProcess,
                        ProcessAccessRights.CreateProcess, false);
                    attr.AddValue(parentProcess);

                    SafeMemoryBuffer val = new SafeMemoryBuffer(IntPtr.Size);
                    attr.AddValue(val);

                    Marshal.WriteIntPtr(val.DangerousGetHandle(), parentProcess.DangerousGetHandle());
                    Kernel32.UpdateProcThreadAttribute(attr,
                        Helpers.ProcessThreadAttribute.PROC_THREAD_ATTRIBUTE_PARENT_PROCESS, val, (UIntPtr)val.Length);
                }

                if (startupInfo.ConPTY.DangerousGetHandle() != IntPtr.Zero)
                {
                    Kernel32.UpdateProcThreadAttribute(attr,
                        Helpers.ProcessThreadAttribute.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                        new SafeNativeHandle(startupInfo.ConPTY.DangerousGetHandle(), false), (UIntPtr)IntPtr.Size);
                }

                if (startupInfo.InheritedHandles.Length > 0 || startupInfo.InheritStdioHandles)
                {
                    // We use a hashset so we don't duplicate handles which will cause a failure
                    HashSet<IntPtr> handles = new HashSet<IntPtr>();

                    foreach (SafeHandle handle in startupInfo.InheritedHandles)
                    {
                        const Helpers.HandleFlags flags = Helpers.HandleFlags.HANDLE_FLAG_INHERIT;

                        // Can only mark them as inherited if the handle is for the current process. If a handle isn't
                        // inheritable then the CreateProcess call will fail with invalid parameter.
                        if (startupInfo.ParentProcess == 0)
                            Kernel32.SetHandleInformation(handle, flags, flags);

                        handles.Add(handle.DangerousGetHandle());
                    }

                    // If we are explicitly inheriting some handles we need to ensure our stdio handles are also in
                    // the list so they can be used. These should have already been duplicated when CreateProcess
                    // prepared them for STARTUPINFO.
                    foreach (SafeHandle pipe in new[] { stdin, stdout, stderr })
                    {
                        if (pipe.IsInvalid)
                            continue;

                        handles.Add(pipe.DangerousGetHandle());
                    }

                    int valueSize = IntPtr.Size * handles.Count;
                    SafeMemoryBuffer val = new SafeMemoryBuffer(valueSize);
                    attr.values.Add(val);

                    IntPtr valOffset = val.DangerousGetHandle();
                    foreach (IntPtr handle in handles)
                    {
                        Marshal.WriteIntPtr(valOffset, handle);
                        valOffset = IntPtr.Add(valOffset, IntPtr.Size);
                    }

                    Kernel32.UpdateProcThreadAttribute(attr,
                        Helpers.ProcessThreadAttribute.PROC_THREAD_ATTRIBUTE_HANDLE_LIST, val, (UIntPtr)valueSize);
                }

                if (startupInfo.JobList.Length > 0)
                {
                    HashSet<IntPtr> handles = startupInfo.JobList
                        .Select(j => j.DangerousGetHandle())
                        .ToHashSet();

                    int valueSize = IntPtr.Size * handles.Count;
                    SafeMemoryBuffer val = new SafeMemoryBuffer(valueSize);
                    attr.values.Add(val);

                    IntPtr valOffset = val.DangerousGetHandle();
                    foreach (IntPtr handle in handles)
                    {
                        Marshal.WriteIntPtr(valOffset, handle);
                        valOffset = IntPtr.Add(valOffset, IntPtr.Size);
                    }

                    Kernel32.UpdateProcThreadAttribute(attr,
                        Helpers.ProcessThreadAttribute.PROC_THREAD_ATTRIBUTE_JOB_LIST, val, (UIntPtr)valueSize);
                }

                if (startupInfo.ChildProcessPolicy != ChildProcessPolicy.None)
                {
                    SafeMemoryBuffer val = new SafeMemoryBuffer(4);
                    attr.AddValue(val);

                    Marshal.WriteInt32(val.DangerousGetHandle(), (int)startupInfo.ChildProcessPolicy);
                    Kernel32.UpdateProcThreadAttribute(
                        attr,
                        Helpers.ProcessThreadAttribute.PROC_THREAD_ATTRIBUTE_CHILD_PROCESS_POLICY, val,
                        (UIntPtr)val.Length);
                }
            }
            catch
            {
                attr.Dispose();
                throw;
            }

            return attr;
        }

        private static SafeHandle CreateSecurityAttributes(SecurityAttributes? attributes)
        {
            if (attributes == null)
            {
                return Helpers.NULL_HANDLE_VALUE;
            }
            else
            {
                Helpers.SECURITY_ATTRIBUTES attr = new Helpers.SECURITY_ATTRIBUTES()
                {
                    nLength = (UInt32)Marshal.SizeOf(typeof(Helpers.SECURITY_ATTRIBUTES)),
                    lpSecurityDescriptor = IntPtr.Zero,
                    bInheritHandle = attributes.InheritHandle,
                };

                byte[] secBytes = new byte[0];
                if (attributes.SecurityDescriptor != null)
                    secBytes = attributes.SecurityDescriptor.GetSecurityDescriptorBinaryForm();

                int bufferLength = Marshal.SizeOf(attr);
                SafeMemoryBuffer lpAttributes = new SafeMemoryBuffer(bufferLength + secBytes.Length);
                try
                {
                    if (secBytes.Length > 0)
                    {
                        attr.lpSecurityDescriptor = IntPtr.Add(lpAttributes.DangerousGetHandle(), bufferLength);
                        Marshal.Copy(secBytes, 0, attr.lpSecurityDescriptor, secBytes.Length);
                        bufferLength += secBytes.Length;
                    }

                    Marshal.StructureToPtr(attr, lpAttributes.DangerousGetHandle(), false);
                }
                catch
                {
                    lpAttributes.Dispose();
                    throw;
                }

                return lpAttributes;
            }
        }

        private static SafeHandle CreateStringBuffer(string? value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return Helpers.NULL_HANDLE_VALUE;
            else
                return new SafeMemoryBuffer(Marshal.StringToHGlobalUni(value), 0);
        }

        private static string GetObjectName(SafeHandle handle)
        {
            using SafeMemoryBuffer buffer = User32.GetUserObjectInformation(handle,
                Helpers.UserObjectInfoIndex.UOI_NAME);

            return Marshal.PtrToStringUni(buffer.DangerousGetHandle()) ?? "";
        }

        private static SecurityIdentifier GetSIDForStation(SafeHandle token)
        {
            try
            {
                return GetTokenLogonSid(token);
            }
            catch (NativeException e) when (e.NativeErrorCode == (int)Win32ErrorCode.ERROR_NOT_FOUND)
            {
                // Running as SYSTEM will have no LogonSID so just use the user SID as a backup.
                return GetTokenUser(token);
            }
        }

        private static SecurityIdentifier GetTokenUser(SafeHandle handle)
        {
            using SafeMemoryBuffer buffer = Advapi32.GetTokenInformation(handle,
                Helpers.TOKEN_INFORMATION_CLASS.TokenUser);

            var tokenUser = Marshal.PtrToStructure<Helpers.TOKEN_USER>(buffer.DangerousGetHandle());
            return new SecurityIdentifier(tokenUser.User.Sid);
        }

        private static UInt32 GetTokenSessionid(SafeHandle handle)
        {
            using SafeMemoryBuffer buffer = Advapi32.GetTokenInformation(handle,
                Helpers.TOKEN_INFORMATION_CLASS.TokenSessionId);

            unsafe
            {
                return *(uint*)buffer.DangerousGetHandle();
            }
        }

        private static SecurityIdentifier GetTokenLogonSid(SafeHandle handle)
        {
            using SafeMemoryBuffer buffer = Advapi32.GetTokenInformation(handle,
                Helpers.TOKEN_INFORMATION_CLASS.TokenLogonSid);

            var tokenGroups = Marshal.PtrToStructure<Helpers.TOKEN_GROUPS>(buffer.DangerousGetHandle());
            return new SecurityIdentifier(tokenGroups.Groups[0].Sid);
        }

        private static void GrantStationDesktopAccess(SecurityIdentifier identity, IntPtr lpDesktop)
        {
            string? targetObject = Marshal.PtrToStringUni(lpDesktop);
            string? station = null;
            string? desktop = null;

            if (!String.IsNullOrEmpty(targetObject))
            {
                desktop = targetObject;

                if (targetObject.Contains("\\"))
                {
                    string[] split = targetObject.Split(new char[] { '\\' }, 2);
                    station = split[0];
                    desktop = split[1];
                }
            }

            using SafeHandle currentStation = User32.GetProcessWindowStation();
            string currentStationName = GetObjectName(currentStation);

            // The handle from GetProcessWindowStation has an explicit don't close so it doesn't matter that
            // we use this in a using block twice.
            SafeHandle targetStation = currentStation;
            bool otherStation = false;
            if (!(station == null || String.Equals(station, currentStationName,
                StringComparison.OrdinalIgnoreCase)))
            {
                targetStation = User32.OpenWindowStation(station, false,
                    StationAccessRights.ReadControl | StationAccessRights.WriteDAC);
                otherStation = true;
            }

            using (targetStation)
            {
                // Ensures the identity has full access to the target station.
                StationSecurity stationSec = new StationSecurity(targetStation, AccessControlSections.Access);
                stationSec.AddAccessRule(
                    stationSec.AccessRuleFactory(identity, (int)StationAccessRights.AllAccess, false,
                    InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow));
                stationSec.Persist(targetStation, AccessControlSections.Access);

                // If we opened the station we need to set it to the current process station so we can open the
                // requested desktop.
                if (otherStation)
                    User32.SetProcessWindowStation(targetStation);

                try
                {
                    SafeHandle desktopHandle;
                    if (desktop == null)
                    {
                        desktopHandle = User32.GetThreadDesktop(Kernel32.GetCurrentThreadId());
                    }
                    else
                    {
                        const DesktopAccessRights access = DesktopAccessRights.ReadControl |
                            DesktopAccessRights.WriteDAC |
                            DesktopAccessRights.ReadObjects |
                            DesktopAccessRights.WriteObjects;
                        desktopHandle = User32.OpenDesktop(desktop, Helpers.OPEN_DESKTOP_FLAGS.NONE, false, access);
                    }

                    using (desktopHandle)
                    {
                        DesktopSecurity deskSec = new DesktopSecurity(desktopHandle, AccessControlSections.Access);
                        deskSec.AddAccessRule(
                            deskSec.AccessRuleFactory(identity, (int)DesktopAccessRights.AllAccess, false,
                            InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow));
                        deskSec.Persist(desktopHandle, AccessControlSections.Access);
                    }
                }
                finally
                {
                    if (otherStation)
                        User32.SetProcessWindowStation(currentStation);
                }
            }
        }

        private static SafeHandle PrepareStdioHandle(SafeHandle handle, StartupInfo startupInfo)
        {
            if (handle.DangerousGetHandle() == IntPtr.Zero)
                return Helpers.NULL_HANDLE_VALUE;

            if (startupInfo.ParentProcess != 0)
            {
                // We need to duplicate the handle into the new parent process so it can be inherited.
                SafeHandle currentProcess = Kernel32.GetCurrentProcess();
                SafeHandle target = startupInfo.OpenParentProcessHandle(ProcessAccessRights.DupHandle);
                return Kernel32.DuplicateHandle(currentProcess, handle, target, 0, true,
                    Helpers.DuplicateHandleOptions.DUPLICATE_SAME_ACCESS, true);
            }
            else
            {
                // We don't need to duplicate the handle into the target process so the child will inherit it.
                // Just make sure it's inheritable here and return that one.
                const Helpers.HandleFlags flags = Helpers.HandleFlags.HANDLE_FLAG_INHERIT;
                Kernel32.SetHandleInformation(handle, flags, flags);

                // The caller will explicitly dispose of this handle, because we don't have ownership of the handle we
                // don't want to close the underlying handle, that's for whatever created the handle in the first
                // place to do.
                return new SafeNativeHandle(handle.DangerousGetHandle(), false);
            }
        }
    }
}
