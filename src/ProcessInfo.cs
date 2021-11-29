using ProcessEx.Native;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ProcessEx
{
    public class ProcessInfo
    {
        private string? _executable;
        private string? _cmdLine;
        private int _pid;
        private int _tid;
        private Helpers.PROCESS_BASIC_INFORMATION? _basicInfo;

        public string Executable
        {
            get
            {
                _executable ??= Kernel32.QueryFullProcessImageName(Process, Helpers.QueryImageNameFlags.NONE);
                return _executable;
            }
        }

        public string CommandLine
        {
            get
            {
                _cmdLine ??= GetProcessCommandLine();
                return _cmdLine;
            }
        }

        public SafeHandle Process { get; internal set; }

        public SafeHandle Thread { get; internal set; }

        // FIXME: Add AliasProperty Id for Process compat.
        public int ProcessId
        {
            get
            {
                if (_pid == 0) _pid = (int)BasicInfo.UniqueProcessId;
                return _pid;
            }
        }

        public int ParentProcessId
        {
            get
            {
                return (int)BasicInfo.InheritedFromUniqueProcessId;
            }
        }

        public int ThreadId
        {
            get
            {
                if (_tid == 0) _tid = Kernel32.GetThreadId(Thread);
                return _tid;
            }
        }

        public int ExitCode
        {
            get
            {
                int rc = Kernel32.GetExitCodeProcess(Process);
                if (rc == 0x00000103) // STILL_ACTIVE
                    throw new InvalidOperationException("The process is still running");

                return rc;
            }
        }

        private Helpers.PROCESS_BASIC_INFORMATION BasicInfo
        {
            get
            {
                if (_basicInfo == null)
                {
                    using SafeMemoryBuffer buffer = Ntdll.NtQueryInformationProcess(Process,
                        Helpers.ProcessInformationClass.ProcessBasicInformation);

                    _basicInfo = Marshal.PtrToStructure<Helpers.PROCESS_BASIC_INFORMATION>(
                        buffer.DangerousGetHandle());
                }

                return (Helpers.PROCESS_BASIC_INFORMATION)_basicInfo;
            }
        }

        internal ProcessInfo(SafeHandle process, SafeHandle thread, int pid = 0, int tid = 0, string? cmdLine = null)
        {
            Process = process;
            Thread = thread;
            _pid = pid;
            _tid = tid;
            _cmdLine = cmdLine;
        }

        internal ProcessInfo(Helpers.PROCESS_INFORMATION pi, string? cmdLine)
            : this(new SafeNativeHandle(pi.hProcess), new SafeNativeHandle(pi.hThread), pi.dwProcessId,
                pi.dwThreadId, cmdLine) {}

        public static ProcessInfo GetCurrentProcess()
        {
            SafeNativeHandle process = Kernel32.GetCurrentProcess();
            SafeNativeHandle thread = Kernel32.GetCurrentThread();
            int pid = Kernel32.GetProcessId(process);
            int tid = Kernel32.GetThreadId(thread);
            string cmdLine = Kernel32.GetCommandLine();

            return new ProcessInfo(process, thread, pid, tid, cmdLine);
        }

        private string GetProcessCommandLine()
        {
            // First need to get the PEB address of the target process.
            IntPtr pebAddress = BasicInfo.PebBaseAddress;

            // Get the address of RTL_USER_PROCESS_PARAMETERS set at a certain offset in the PEB.
            // FIXME: Determine a better way of getting the offset for RTL_USER_PROCESS_PARAMETERS in the PEB.
            int processParamOffset = 0x20; // 0x10 32-bit OS
            IntPtr ppAddress = IntPtr.Zero;
            using (SafeMemoryBuffer buffer = Kernel32.ReadProcessMemory(Process,
                IntPtr.Add(pebAddress, processParamOffset), IntPtr.Size))
            {
                ppAddress = Marshal.ReadIntPtr(buffer.DangerousGetHandle());
            }

            // Get the actual value for RTL_USER_PROCESS_PARAMETERS and get the CommandLine value.
            using (SafeMemoryBuffer buffer = Kernel32.ReadProcessMemory(Process, ppAddress,
                Marshal.SizeOf(typeof(Helpers.RTL_USER_PROCESS_PARAMETERS))))
            {
                var pp = Marshal.PtrToStructure<Helpers.RTL_USER_PROCESS_PARAMETERS>(buffer.DangerousGetHandle());

                using SafeMemoryBuffer uniString = Kernel32.ReadProcessMemory(Process, pp.CommandLine.Buffer,
                    pp.CommandLine.Length);
                unsafe
                {
                    return Encoding.Unicode.GetString((byte *)uniString.DangerousGetHandle(), uniString.Length);
                }
            }
        }
    }
}
