using ProcessEx.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private ProcessBasicInformation? _basicInfo;
        private PEB? _peb;
        private bool? _isWow64;

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
                _cmdLine ??= ProcessEnvironmentBlock.ProcessParameters.CommandLine;
                return _cmdLine;
            }
        }

        public SafeHandle Process { get; internal set; }

        public SafeHandle Thread { get; internal set; }

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

        public Dictionary<string, string> Environment
        {
            get
            {
                return ProcessEnvironmentBlock.ProcessParameters.Environment;
            }
        }

        private ProcessBasicInformation BasicInfo
        {
            get
            {
                _basicInfo ??= GetProcessBasicInformation();
                return _basicInfo;
            }
        }

        private PEB ProcessEnvironmentBlock
        {
            get
            {
                _peb ??= GetProcessEnvironmentBlock();
                return _peb;
            }
        }

        private bool IsWow64
        {
            get
            {
                _isWow64 ??= Kernel32.IsWow64Process(Process);
                return (bool)_isWow64;
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

        internal static ProcessInfo GetCurrentProcess()
        {
            SafeNativeHandle process = Kernel32.GetCurrentProcess();
            SafeNativeHandle thread = Kernel32.GetCurrentThread();
            int pid = Kernel32.GetProcessId(process);
            int tid = Kernel32.GetThreadId(thread);
            string cmdLine = Kernel32.GetCommandLine();

            return new ProcessInfo(process, thread, pid, tid, cmdLine);
        }

        private ProcessBasicInformation GetProcessBasicInformation()
        {
            if (System.Environment.Is64BitOperatingSystem && !System.Environment.Is64BitProcess && !IsWow64)
            {
                // When the current process is 32-bit but the target process is 64-bit a special function and structure
                // needs to be used.
                using SafeMemoryBuffer buffer = Ntdll.NtWow64QueryInformationProcess64(Process,
                    Helpers.ProcessInformationClass.ProcessBasicInformation);

                Helpers.PROCESS_BASIC_INFORMATION_64 bi = Marshal.PtrToStructure<Helpers.PROCESS_BASIC_INFORMATION_64>(
                    buffer.DangerousGetHandle());

                return new ProcessBasicInformation(bi);
            }
            else
            {
                using SafeMemoryBuffer buffer = Ntdll.NtQueryInformationProcess(Process,
                    Helpers.ProcessInformationClass.ProcessBasicInformation);

                Helpers.PROCESS_BASIC_INFORMATION bi = Marshal.PtrToStructure<Helpers.PROCESS_BASIC_INFORMATION>(
                    buffer.DangerousGetHandle());

                ProcessBasicInformation info = new ProcessBasicInformation(bi);

                if (System.Environment.Is64BitProcess)
                {
                    using SafeMemoryBuffer wowBuffer = Ntdll.NtQueryInformationProcess(Process,
                        Helpers.ProcessInformationClass.ProcessWow64Information);

                    IntPtr pepBase = Marshal.ReadIntPtr(wowBuffer.DangerousGetHandle());
                    if (pepBase != IntPtr.Zero)
                    {
                        _isWow64 = true;
                        info.PebBaseAddress = (Int64)pepBase;
                    }
                }

                return info;
            }
        }

        private PEB GetProcessEnvironmentBlock()
        {
            bool current32 = true;
            bool target32 = true;
            if (System.Environment.Is64BitOperatingSystem)
            {
                current32 = !System.Environment.Is64BitProcess;
                target32 = IsWow64;
            }

            if (current32 == target32)
            {
                // Same process architecture - use the native structs
                using SafeMemoryBuffer buffer = Kernel32.ReadProcessMemory(Process, (IntPtr)BasicInfo.PebBaseAddress,
                    (IntPtr)Marshal.SizeOf(typeof(Helpers.PEB)));

                Helpers.PEB peb = Marshal.PtrToStructure<Helpers.PEB>(buffer.DangerousGetHandle());
                return new PEB(Process, peb);
            }
            else if (target32)
            {
                // Current process is 64-bit while the target is 32-bit
                using SafeMemoryBuffer buffer = Kernel32.ReadProcessMemory(Process, (IntPtr)BasicInfo.PebBaseAddress,
                    (IntPtr)Marshal.SizeOf(typeof(Helpers.PEB)));

                Helpers.PEB_32 peb = Marshal.PtrToStructure<Helpers.PEB_32>(buffer.DangerousGetHandle());
                return new PEB(Process, peb);
            }
            else
            {
                // Current process is 32-bit while the target is 64-bit, also requires special function
                using SafeMemoryBuffer buffer = Ntdll.NtWow64ReadVirtualMemory64(Process, BasicInfo.PebBaseAddress,
                    Marshal.SizeOf(typeof(Helpers.PEB_64)));

                Helpers.PEB_64 peb = Marshal.PtrToStructure<Helpers.PEB_64>(buffer.DangerousGetHandle());
                return new PEB(Process, peb);
            }
        }
    }

    internal class PEB
    {
        public bool BeingDebugged { get; set; }
        public ProcessParameters ProcessParameters { get; set; }
        public UInt32 SessionId { get; set; }

        public PEB(SafeHandle process, Helpers.PEB peb)
        {
            BeingDebugged = peb.BeingDebugged != 0;
            ProcessParameters = new ProcessParameters(process, peb.ProcessParameters);
            SessionId = peb.SessionId;
        }

        public PEB(SafeHandle process, Helpers.PEB_32 peb)
        {
            BeingDebugged = peb.BeingDebugged != 0;
            ProcessParameters = new ProcessParameters(process, peb.ProcessParameters);
            SessionId = peb.SessionId;
        }

        public PEB(SafeHandle process, Helpers.PEB_64 peb)
        {
            BeingDebugged = peb.BeingDebugged != 0;
            ProcessParameters = new ProcessParameters(process, peb.ProcessParameters);
            SessionId = peb.SessionId;
        }
    }

    internal class ProcessParameters
    {
        public string ImagePathName { get; set; }
        public string CommandLine { get; set; }
        public Dictionary<string, string> Environment { get; set; }

        public ProcessParameters(SafeHandle process, IntPtr ptr)
        {
            using SafeMemoryBuffer buffer = Kernel32.ReadProcessMemory(process, ptr,
                (IntPtr)Marshal.SizeOf(typeof(Helpers.RTL_USER_PROCESS_PARAMETERS)));
            var pp = Marshal.PtrToStructure<Helpers.RTL_USER_PROCESS_PARAMETERS>(buffer.DangerousGetHandle());

            ImagePathName = ConvertUnicodeString(process, pp.ImagePathName);
            CommandLine = ConvertUnicodeString(process, pp.CommandLine);
            Environment = GetEnvironment(process, pp.Environment);
        }

        public ProcessParameters(SafeHandle process, int ptr)
        {
            using SafeMemoryBuffer buffer = Kernel32.ReadProcessMemory(process, (IntPtr)ptr,
                (IntPtr)Marshal.SizeOf(typeof(Helpers.RTL_USER_PROCESS_PARAMETERS_32)));
            var pp = Marshal.PtrToStructure<Helpers.RTL_USER_PROCESS_PARAMETERS_32>(buffer.DangerousGetHandle());

            ImagePathName = ConvertUnicodeString(process, pp.ImagePathName);
            CommandLine = ConvertUnicodeString(process, pp.CommandLine);
            Environment = GetEnvironment(process, (IntPtr)pp.Environment);
        }

        public ProcessParameters(SafeHandle process, Int64 ptr)
        {
            using SafeMemoryBuffer buffer = Ntdll.NtWow64ReadVirtualMemory64(process, ptr,
                Marshal.SizeOf(typeof(Helpers.RTL_USER_PROCESS_PARAMETERS_64)));
            var pp = Marshal.PtrToStructure<Helpers.RTL_USER_PROCESS_PARAMETERS_64>(buffer.DangerousGetHandle());

            ImagePathName = ConvertUnicodeString(process, pp.ImagePathName);
            CommandLine = ConvertUnicodeString(process, pp.CommandLine);
            Environment = GetEnvironment(process, pp.Environment);
        }

        private static string ConvertUnicodeString(SafeHandle process, Helpers.UNICODE_STRING uniStr)
        {
            using SafeMemoryBuffer uniBuffer = Kernel32.ReadProcessMemory(process, uniStr.Buffer,
                (IntPtr)uniStr.Length);
            unsafe
            {
                return Encoding.Unicode.GetString((byte *)uniBuffer.DangerousGetHandle(), uniBuffer.Length);
            }
        }

        private static string ConvertUnicodeString(SafeHandle process, Helpers.UNICODE_STRING_32 uniStr)
        {
            using SafeMemoryBuffer uniBuffer = Kernel32.ReadProcessMemory(process, (IntPtr)uniStr.Buffer,
                (IntPtr)uniStr.Length);
            unsafe
            {
                return Encoding.Unicode.GetString((byte *)uniBuffer.DangerousGetHandle(), uniBuffer.Length);
            }
        }

        private static string ConvertUnicodeString(SafeHandle process, Helpers.UNICODE_STRING_64 uniStr)
        {
            using SafeMemoryBuffer uniBuffer = Ntdll.NtWow64ReadVirtualMemory64(process, uniStr.Buffer,
                uniStr.Length);
            unsafe
            {
                return Encoding.Unicode.GetString((byte *)uniBuffer.DangerousGetHandle(), uniBuffer.Length);
            }
        }

        private static Dictionary<string, string> GetEnvironment(SafeHandle process, IntPtr environment)
        {
            Helpers.MEMORY_BASIC_INFORMATION mi = Kernel32.VirtualQueryEx(process, environment);
            Int64 regionOffset = (Int64)environment - (Int64)mi.BaseAddress;
            Debug.Assert(regionOffset <= Int32.MaxValue);
            IntPtr blockSize = IntPtr.Subtract(mi.RegionSize, (int)regionOffset);

            using SafeMemoryBuffer envBlock = Kernel32.ReadProcessMemory(process, environment, blockSize);
            return ProcessRunner.ConvertEnvironmentBlock(envBlock);
        }

        private static Dictionary<string, string> GetEnvironment(SafeHandle process, Int64 environment)
        {
            // Win 10 dropped NtWow64QueryVirtualMemory64 which could be used to get the memory size. Instead us 32KiB
            // as the memory size which is the max amount of memory that can be stored in the environment block.
            // This isn't ideal but it's better than nothing.
            int blockSize = 32768;
            using SafeMemoryBuffer envBlock = Ntdll.NtWow64ReadVirtualMemory64(process, environment, blockSize);
            return ProcessRunner.ConvertEnvironmentBlock(envBlock);
        }
    }

    internal class ProcessBasicInformation
    {
        public int ExitCode { get; set; }
        public Int64 PebBaseAddress { get; set; }
        public int UniqueProcessId { get; set; }
        public int InheritedFromUniqueProcessId { get; set; }

        public ProcessBasicInformation(Helpers.PROCESS_BASIC_INFORMATION bi)
        {
            ExitCode = (int)bi.ExitStatus;
            PebBaseAddress = (Int64)bi.PebBaseAddress;
            UniqueProcessId = (int)bi.UniqueProcessId;
            InheritedFromUniqueProcessId = (int)bi.InheritedFromUniqueProcessId;
        }

        public ProcessBasicInformation(Helpers.PROCESS_BASIC_INFORMATION_64 bi)
        {
            ExitCode = (int)bi.ExitStatus;
            PebBaseAddress = bi.PebBaseAddress;
            UniqueProcessId = (int)bi.UniqueProcessId;
            InheritedFromUniqueProcessId = (int)bi.InheritedFromUniqueProcessId;
        }
    }
}
