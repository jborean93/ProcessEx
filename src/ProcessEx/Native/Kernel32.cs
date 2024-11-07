using Microsoft.Win32.SafeHandles;
using ProcessEx.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;

namespace ProcessEx.Native
{
    internal partial class Helpers
    {
        public static SafeNativeHandle NULL_HANDLE_VALUE => new SafeNativeHandle(IntPtr.Zero, false);

        public static SafeNativeHandle INVALID_HANDLE_VALUE => new SafeNativeHandle((IntPtr)(-1), false);

        [StructLayout(LayoutKind.Sequential)]
        public struct COORD
        {
            public Int16 X;
            public Int16 Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_ASSOCIATE_COMPLETION_PORT
        {
            public IntPtr CompletionKey;
            public IntPtr CompletionPort;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public MemoryProtection AllocationProtect;
            // This is documented but is only valid for x64. 32-bit processes do not have this field and can safely
            // be omitted for x64 as it fits into the byte alignment padding anyway.
            // public UInt16 PartitionId;
            public IntPtr RegionSize;
            public MemoryState State;
            public MemoryProtection Protect;
            public MemoryType Type;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public UInt32 nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFOEX
        {
            public STARTUPINFOW startupInfo;
            public IntPtr lpAttributeList;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFOW
        {
            public UInt32 cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public ConsoleFill dwFillAttribute;
            public StartupInfoFlags dwFlags;
            public WindowStyle wShowWindow;
            public UInt16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [Flags]
        public enum DuplicateHandleOptions : uint
        {
            NONE = 0x0000000,
            DUPLICATE_CLOSE_SOURCE = 0x00000001,
            DUPLICATE_SAME_ACCESS = 0x00000002,
        }

        [Flags]
        public enum FileFlags : uint
        {
            NONE = 0,
            FILE_FLAG_OPEN_NO_RECALL = 0x00100000,
            FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000,
            FILE_FLAG_SESSION_AWARE = 0x00800000,
            FILE_FLAG_POSIX_SEMANTICS = 0x01000000,
            FILE_FLAG_BACKUP_SEMANTICS = 0x02000000,
            FILE_FLAG_DELETE_ON_CLOSE = 0x04000000,
            FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000,
            FILE_FLAG_RANDOM_ACCESS = 0x10000000,
            FILE_FLAG_NO_BUFFERING = 0x20000000,
            FILE_FLAG_OVERLAPPED = 0x40000000,
            FILE_FLAG_WRITE_THROUGH = 0x80000000,
        }

        [Flags]
        public enum HandleFlags : uint
        {
            NONE = 0x00000000,
            HANDLE_FLAG_INHERIT = 0x00000001,
            HANDLE_FLAG_PROTECT_FROM_CLOSE = 0x00000002,
        }

        public enum JobObjectInformationClass : uint
        {
            JobObjectBasicAccountingInformation = 1,
            JobObjectBasicLimitInformation = 2,
            JobObjectBasicProcessIdList = 3,
            JobObjectBasicUIRestrictions = 4,
            JobObjectSecurityLimitInformation = 5,
            JobObjectEndOfJobTimeInformation = 6,
            JobObjectAssociateCompletionPortInformation = 7,
            JobObjectBasicAndIoAccountingInformation = 8,
            JobObjectExtendedLimitInformation = 9,
            JobObjectJobSetInformation = 10,
            JobObjectGroupInformation = 11,
            JobObjectNotificationLimitInformation = 12,
            JobObjectLimitViolationInformation = 13,
            JobObjectGroupInformationEx = 14,
            JobObjectCpuRateControlInformation = 15,
            JobObjectCompletionFilter = 16,
            JobObjectCompletionCounter = 17,
            JobObjectReserved1Information = 18,
            JobObjectReserved2Information = 19,
            JobObjectReserved3Information = 20,
            JobObjectReserved4Information = 21,
            JobObjectReserved5Information = 22,
            JobObjectReserved6Information = 23,
            JobObjectReserved7Information = 24,
            JobObjectReserved8Information = 25,
            JobObjectReserved9Information = 26,
            JobObjectReserved10Information = 27,
            JobObjectReserved11Information = 28,
            JobObjectReserved12Information = 29,
            JobObjectReserved13Information = 30,
            JobObjectReserved14Information = 31,
            JobObjectNetRateControlInformation = 32,
            JobObjectNotificationLimitInformation2 = 33,
            JobObjectLimitViolationInformation2 = 34,
            JobObjectCreateSilo = 35,
            JobObjectSiloBasicInformation = 36,
            JobObjectReserved15Information = 37,
            JobObjectReserved16Information = 38,
            JobObjectReserved17Information = 39,
            JobObjectReserved18Information = 40,
            JobObjectReserved19Information = 41,
            JobObjectReserved20Information = 42,
            JobObjectReserved21Information = 43,
            JobObjectReserved22Information = 44,
            JobObjectReserved23Information = 45,
            JobObjectReserved24Information = 46,
            JobObjectReserved25Information = 47,
        }

        [Flags]
        public enum MemoryProtection : uint
        {
            NONE = 0x00000000,
            PAGE_NOACCESS = 0x00000001,
            PAGE_READONLY = 0x00000002,
            PAGE_READWRITE = 0x00000004,
            PAGE_WRITECOPY = 0x00000008,
            PAGE_EXECUTE = 0x00000010,
            PAGE_EXECUTE_READ = 0x00000020,
            PAGE_EXECUTE_READWRITE = 0x00000040,
            PAGE_EXECUTE_WRITECOPY = 0x00000080,
            PAGE_GUARD = 0x00000100,
            PAGE_NOCACHE = 0x00000200,
            PAGE_WRITECOMBINE = 0x00000400,
            PAGE_TARGETS_INVALID = 0x40000000,
            PAGE_TARGETS_NO_UPDATE = 0x40000000,
        }

        public enum MemoryState : uint
        {
            MEM_COMMIT = 0x00001000,
            MEM_RESERVE = 0x00002000,
            MEM_FREE = 0x00010000,
        }

        public enum MemoryType : uint
        {
            MEM_PRIVATE = 0x00020000,
            MEM_MAPPED = 0x00040000,
            MEM_IMAGE = 0x01000000,
        }

        public enum ProcessThreadAttribute : uint
        {
            PROC_THREAD_ATTRIBUTE_PARENT_PROCESS = 0x00020000,
            PROC_THREAD_ATTRIBUTE_HANDLE_LIST = 0x00020002,
            PROC_THREAD_ATTRIBUTE_GROUP_AFFINITY = 0x00030003,
            PROC_THREAD_ATTRIBUTE_PREFERRED_NODE = 0x00020004,
            PROC_THREAD_ATTRIBUTE_IDEAL_PROCESSOR = 0x00030005,
            PROC_THREAD_ATTRIBUTE_UMS_THREAD = 0x00030006,
            PROC_THREAD_ATTRIBUTE_MITIGATION_POLICY = 0x00020007,
            PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES = 0x00020009,
            PROC_THREAD_ATTRIBUTE_PROTECTION_LEVEL = 0x0002000B,
            PROC_THREAD_ATTRIBUTE_JOB_LIST = 0x0002000D,
            PROC_THREAD_ATTRIBUTE_CHILD_PROCESS_POLICY = 0x0002000E,
            PROC_THREAD_ATTRIBUTE_ALL_APPLICATION_PACKAGES_POLICY = 0x0002000F,
            PROC_THREAD_ATTRIBUTE_WIN32K_FILTER = 0x00020010,
            PROC_THREAD_ATTRIBUTE_SAFE_OPEN_PROMPT_ORIGIN_CLAIM = 0x00020011,
            PROC_THREAD_ATTRIBUTE_DESKTOP_APP_POLICY = 0x00020012,
            PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016,
            PROC_THREAD_ATTRIBUTE_MITIGATION_AUDIT_POLICY = 0x00020018,
        }

        [Flags]
        public enum PseudoConsoleCreateFlags : uint
        {
            NONE = 0x00000000,
            PSEUDOCONSOLE_INHERIT_CURSOR = 0x00000001,
        }

        [Flags]
        public enum QueryImageNameFlags : uint
        {
            NONE = 0x00000000,
            PROCESS_NAME_NATIVE = 0x00000001,
        }

        public enum WaitResult : uint
        {
            WAIT_OBJECT_0 = 0x00000000,
            WAIT_ABANDONED = 0x00000080,
            WAIT_TIMEOUT = 0x00000102,
            WAIT_FAILED = 0xFFFFFFFF,
        }
    }

    internal static class Kernel32
    {
        [DllImport("Kernel32.dll", EntryPoint = "AssignProcessToJobObject", SetLastError = true)]
        private static extern bool NativeAssignProcessToJobObject(
            SafeHandle hJob,
            SafeHandle hProcess);

        public static void AssignProcessToJobObject(SafeHandle job, SafeHandle process)
        {
            if (!NativeAssignProcessToJobObject(job, process))
                throw new NativeException("AssignProcessToJobObject");
        }

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(
            IntPtr hObject);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern Int32 ClosePseudoConsole(
            IntPtr hPC);

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            FileSystemRights dwDesiredAccess,
            FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            FileMode dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        public static SafeFileHandle CreateFile(
            string path,
            FileSystemRights access,
            FileShare share,
            FileMode mode,
            FileAttributes attributes,
            Helpers.FileFlags flags)
        {
            uint flagsAndAttributes = (uint)attributes | (uint)flags;
            SafeFileHandle handle = CreateFileW(
                path,
                access,
                share,
                IntPtr.Zero,
                mode,
                flagsAndAttributes,
                IntPtr.Zero);
            if (handle.IsInvalid)
            {
                throw new NativeException("CreateFileW", Marshal.GetLastWin32Error());
            }

            return handle;
        }

        [DllImport("Kernel32.dll", EntryPoint = "CreateIoCompletionPort", SetLastError = true)]
        private static extern SafeNativeHandle NativeCreateIoCompletionPort(
            SafeHandle FileHandle,
            SafeHandle ExistingCompletionPort,
            UIntPtr CompletionKey,
            UInt32 NumberOfConcurrentThreads);

        public static SafeNativeHandle CreateIoCompletionPort(SafeHandle handle, SafeHandle existingCompletionPort,
            UIntPtr completionKey, UInt32 numberOfThreads)
        {
            SafeNativeHandle ioPort = NativeCreateIoCompletionPort(handle, existingCompletionPort,
                completionKey, numberOfThreads);

            if (ioPort.DangerousGetHandle() == IntPtr.Zero)
                throw new NativeException("CreateIoCompletionPort");

            return ioPort;
        }

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeNativeHandle CreateJobObjectW(
            SafeHandle lpJobAttributes,
            string? lpName);

        public static SafeNativeHandle CreateJobObject(string? name, SafeHandle security)
        {
            SafeNativeHandle job = CreateJobObjectW(security, name);
            if (job.DangerousGetHandle() == IntPtr.Zero)
                throw new NativeException("CreateJobObject");

            return job;
        }

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessW(
            [MarshalAs(UnmanagedType.LPWStr)] string? lpApplicationName,
            StringBuilder lpCommandLine,
            SafeHandle lpProcessAttributes,
            SafeHandle lpThreadAttributes,
            bool bInheritHandles,
            CreationFlags dwCreationFlags,
            SafeHandle lpEnvironment,
            [MarshalAs(UnmanagedType.LPWStr)] string? lpCurrentDirectory,
            ref Helpers.STARTUPINFOEX lpStartupInfo,
            out Helpers.PROCESS_INFORMATION lpProcessInformation);

        public static ProcessInfo CreateProcess(string? applicationName, string? commandLine,
            SafeHandle processAttributes, SafeHandle threadAttributes, bool inherit, CreationFlags creationFlags,
            SafeHandle environment, string? currentDirectory, Helpers.STARTUPINFOEX startupInfo)
        {
            StringBuilder cmdLine = new StringBuilder(commandLine);
            if (!CreateProcessW(applicationName, cmdLine, processAttributes, threadAttributes, inherit,
                creationFlags, environment, currentDirectory, ref startupInfo, out var pi))
            {
                throw new NativeException("CreateProcess");
            }

            return new ProcessInfo(pi, cmdLine.ToString());
        }

        [DllImport("Kernel32.dll", EntryPoint = "CreatePseudoConsole", SetLastError = true)]
        private static extern Int32 NativeCreatePseudoConsole(
            Helpers.COORD size,
            SafeHandle hInput,
            SafeHandle hOutput,
            Helpers.PseudoConsoleCreateFlags dwFlags,
            out SafeConsoleHandle phPC);

        public static SafeConsoleHandle CreatePseudoConsole(Helpers.COORD size, SafeHandle input, SafeHandle output,
            Helpers.PseudoConsoleCreateFlags flags)
        {
            Int32 res = NativeCreatePseudoConsole(size, input, output, flags, out var handle);
            if (res != 0)
                throw new NativeException("CreatePseudoConsole", res);

            return handle;
        }

        [DllImport("Kernel32.dll")]
        public static extern void DeleteProcThreadAttributeList(
            IntPtr lpAttributeList);

        [DllImport("Kernel32.dll", EntryPoint = "DuplicateHandle", SetLastError = true)]
        private static extern bool NativeDuplicateHandle(
            SafeHandle hSourceProcessHandle,
            SafeHandle hSourceHandle,
            SafeHandle hTargetProcessHandle,
            out IntPtr lpTargetHandle,
            UInt32 dwDesiredAccess,
            bool bInheritHandle,
            Helpers.DuplicateHandleOptions dwOptions);

        public static SafeDuplicateHandle DuplicateHandle(SafeHandle sourceProcess, SafeHandle sourceHandle,
            SafeHandle? targetProcess, UInt32 access, bool inherit, Helpers.DuplicateHandleOptions options,
            bool ownsHandle)
        {
            if (targetProcess == null)
            {
                targetProcess = new SafeNativeHandle(IntPtr.Zero, false);
                // If closing the duplicate then mark the returned handle so it doesn't try to close itself again.
                ownsHandle = (options & Helpers.DuplicateHandleOptions.DUPLICATE_CLOSE_SOURCE) == 0;
            }

            if (!NativeDuplicateHandle(sourceProcess, sourceHandle, targetProcess, out var dup, access, inherit,
                options))
            {
                throw new NativeException("DuplicateHandle");
            }

            return new SafeDuplicateHandle(dup, targetProcess, ownsHandle);
        }

        [DllImport("Psapi.dll", EntryPoint = "EnumProcesses", SetLastError = true)]
        private static extern bool NativeEnumProcesses(
            Int32[] lpidProcess,
            Int32 cb,
            out Int32 lpcbNeeded);

        public static Int32[] EnumProcesses()
        {
            int sizeNeeded = 0;
            while (true)
            {
                // Increase buffer by 256 pids at a time.
                int cb = sizeNeeded + 1024;
                int[] pids = new int[cb / 4];
                if (!NativeEnumProcesses(pids, cb, out sizeNeeded))
                    throw new NativeException("EnumProcesses");

                if (cb == sizeNeeded)
                    continue;

                return pids;
            }
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetCommandLineW();

        public static string GetCommandLine()
        {
            return Marshal.PtrToStringUni(GetCommandLineW()) ?? "";
        }

        [DllImport("Kernel32.dll", EntryPoint = "GetCurrentProcess")]
        private static extern IntPtr NativeGetCurrentProcess();

        public static SafeNativeHandle GetCurrentProcess()
        {
            return new SafeNativeHandle(NativeGetCurrentProcess(), false);
        }

        [DllImport("Kernel32.dll", EntryPoint = "GetCurrentThread")]
        private static extern IntPtr NativeGetCurrentThread();

        public static SafeNativeHandle GetCurrentThread()
        {
            return new SafeNativeHandle(NativeGetCurrentThread(), false);
        }

        [DllImport("Kernel32.dll")]
        public static extern Int32 GetCurrentThreadId();

        [DllImport("Kernel32.dll", EntryPoint = "GetExitCodeProcess", SetLastError = true)]
        private static extern bool NativeGetExitCodeProcess(
            SafeHandle hProcess,
            out Int32 lpExitCode);

        public static Int32 GetExitCodeProcess(SafeHandle process)
        {
            if (!NativeGetExitCodeProcess(process, out var rc))
                throw new NativeException("GetExitCodeProcess");

            return rc;
        }

        [DllImport("Kernel32.dll", EntryPoint = "GetProcessId", SetLastError = true)]
        private static extern Int32 NativeGetProcessId(
            SafeHandle Process);

        public static Int32 GetProcessId(SafeHandle process)
        {
            int tid = NativeGetProcessId(process);
            if (tid == 0)
                throw new NativeException("GetProcessId");

            return tid;
        }

        [DllImport("Kernel32.dll", EntryPoint = "GetQueuedCompletionStatus", SetLastError = true)]
        private static extern bool NativeGetQueuedCompletionStatus(
            SafeHandle CompletionPort,
            out UInt32 lpNumberOfBytesTransferred,
            out UIntPtr lpCompletionKey,
            out IntPtr lpOverlapped,
            UInt32 dwMilliseconds);

        public static void GetQueuedCompletionStatus(SafeHandle completionPort, UInt32 timeoutMS,
            out UInt32 bytesTransferred, out UIntPtr completionKey, out IntPtr overlapped)
        {
            if (!NativeGetQueuedCompletionStatus(completionPort, out bytesTransferred, out completionKey,
                out overlapped, timeoutMS))
            {
                throw new NativeException("GetQueuedCompletionStatus");
            }
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern void GetStartupInfoW(
            ref Helpers.STARTUPINFOW lpStartupInfo);

        public static Helpers.STARTUPINFOW GetStartupInfo()
        {
            Helpers.STARTUPINFOW si = new Helpers.STARTUPINFOW();
            si.cb = (uint)Marshal.SizeOf(si);
            GetStartupInfoW(ref si);

            return si;
        }

        [DllImport("Kernel32.dll", EntryPoint = "GetThreadId", SetLastError = true)]
        private static extern Int32 NativeGetThreadId(
            SafeHandle Thread);

        public static Int32 GetThreadId(SafeHandle thread)
        {
            int tid = NativeGetThreadId(thread);
            if (tid == 0)
                throw new NativeException("GetThreadId");

            return tid;
        }

        [DllImport("Kernel32.dll", EntryPoint = "InitializeProcThreadAttributeList", SetLastError = true)]
        private static extern bool NativeInitializeProcThreadAttributeList(
            IntPtr lpAttributeList,
            Int32 dwAttributeCount,
            UInt32 dwFlags,
            ref IntPtr lpSize);

        public static SafeProcThreadAttribute InitializeProcThreadAttributeList(int count)
        {
            IntPtr size = IntPtr.Zero;
            NativeInitializeProcThreadAttributeList(IntPtr.Zero, count, 0, ref size);

            IntPtr h = Marshal.AllocHGlobal((int)size);
            try
            {
                if (!NativeInitializeProcThreadAttributeList(h, count, 0, ref size))
                    throw new NativeException("InitializeProcThreadAttributeList");

                return new SafeProcThreadAttribute(h, true);
            }
            catch
            {
                Marshal.FreeHGlobal(h);
                throw;
            }
        }

        [DllImport("Kernel32.dll", EntryPoint = "IsWow64Process", SetLastError = true)]
        private static extern bool NativeIsWow64Process(
            SafeHandle hProcess,
            out bool Wow64Process);

        public static bool IsWow64Process(SafeHandle process)
        {
            if (!NativeIsWow64Process(process, out var isWow64))
                throw new NativeException("IsWow64Process");

            return isWow64;
        }

        [DllImport("Kernel32.dll", EntryPoint = "OpenProcess", SetLastError = true)]
        private static extern SafeNativeHandle NativeOpenProcess(
            ProcessAccessRights dwDesiredAccess,
            bool bInheritHandle,
            Int32 dwProcessId);

        public static SafeNativeHandle OpenProcess(int processId, ProcessAccessRights access, bool inherit)
        {
            SafeNativeHandle handle = NativeOpenProcess(access, inherit, processId);
            if (handle.DangerousGetHandle() == IntPtr.Zero)
                throw new NativeException("OpenProcess");

            return handle;
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool QueryFullProcessImageNameW(
            SafeHandle hProcess,
            Helpers.QueryImageNameFlags dwFlags,
            StringBuilder lpExeName,
            ref Int32 lpdwSize);

        public static string QueryFullProcessImageName(SafeHandle handle, Helpers.QueryImageNameFlags flags)
        {
            int size = 0;
            while (true)
            {
                size += 1024;

                StringBuilder name = new StringBuilder(size);
                if (QueryFullProcessImageNameW(handle, flags, name, ref size))
                    return name.ToString(0, size);

                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != (int)Win32ErrorCode.ERROR_INSUFFICIENT_BUFFER)
                    throw new NativeException("QueryFullProcessImageName");
            }
        }

        [DllImport("Kernel32.dll", EntryPoint = "ReadProcessMemory", SetLastError = true)]
        private static extern bool NativeReadProcessMemory(
            SafeHandle hProcess,
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            IntPtr nSize,
            out IntPtr lpNumberOfBytesRead);

        public static SafeMemoryBuffer ReadProcessMemory(SafeHandle process, IntPtr address, IntPtr length)
        {
            SafeMemoryBuffer buffer = new SafeMemoryBuffer((int)length);
            if (!NativeReadProcessMemory(process, address, buffer.DangerousGetHandle(), length, out var read))
            {
                buffer.Dispose();
                throw new NativeException("ReadProcessMemory");
            }

            buffer.Length = (int)read;
            return buffer;
        }

        [DllImport("Kernel32.dll", EntryPoint = "ResizePseudoConsole")]
        private static extern Int32 NativeResizePseudoConsole(
            SafeHandle hPC,
            Helpers.COORD size);

        public static void ResizePseudoConsole(SafeHandle pty, Helpers.COORD size)
        {
            int res = NativeResizePseudoConsole(pty, size);
            if (res != 0)
                throw new NativeException("ResizePseudoConsole", res);
        }

        [DllImport("Kernel32.dll", EntryPoint = "ResumeThread", SetLastError = true)]
        private static extern UInt32 NativeResumeThread(
            SafeHandle hThread);

        public static UInt32 ResumeThread(SafeHandle thread)
        {
            UInt32 res = NativeResumeThread(thread);
            if (res == 0xFFFFFFFF)
                throw new NativeException("ResumeThread");

            return res;
        }

        [DllImport("Kernel32.dll", EntryPoint = "SetHandleInformation", SetLastError = true)]
        private static extern bool NativeSetHandleInformation(
            SafeHandle hObject,
            Helpers.HandleFlags dwMask,
            Helpers.HandleFlags dwFlags);

        public static void SetHandleInformation(SafeHandle obj, Helpers.HandleFlags mask,
            Helpers.HandleFlags flags)
        {
            if (!NativeSetHandleInformation(obj, mask, flags))
                throw new NativeException("SetHandleInformation");
        }

        [DllImport("Kernel32.dll", EntryPoint = "SetInformationJobObject", SetLastError = true)]
        private static extern bool NativeSetInformationJobObject(
            SafeHandle hJob,
            Helpers.JobObjectInformationClass JobObjectInformationClass,
            SafeHandle lpJobObjectInformation,
            Int32 cbJobObjectInformationLength);

        public static void SetInformationJobObject(SafeHandle job, Helpers.JobObjectInformationClass infoClass,
            SafeHandle info, int infoLength)
        {
            if (!NativeSetInformationJobObject(job, infoClass, info, infoLength))
                throw new NativeException("SetInformationJobObject");
        }

        [DllImport("Kernel32.dll", EntryPoint = "TerminateProcess", SetLastError = true)]
        private static extern bool NativeTerminateProcess(
            SafeHandle hProcess,
            int uExitCode);

        public static void TerminateProcess(SafeHandle process, int exitCode)
        {
            if (!NativeTerminateProcess(process, exitCode))
            {
                throw new NativeException("TerminateProcess");
            }
        }

        [DllImport("Kernel32.dll", EntryPoint = "UpdateProcThreadAttribute", SetLastError = true)]
        private static extern bool NativeUpdateProcThreadAttribute(
            SafeHandle lpAttributeList,
            UInt32 dwFlags,
            UIntPtr Attribute,
            SafeHandle lpValue,
            UIntPtr cbSize,
            IntPtr lpPreviousValue,
            IntPtr lpReturnSize);

        public static void UpdateProcThreadAttribute(SafeProcThreadAttribute attributeList,
            Helpers.ProcessThreadAttribute attr, SafeHandle value, UIntPtr size)
        {
            if (!NativeUpdateProcThreadAttribute(attributeList, 0, (UIntPtr)attr, value, size, IntPtr.Zero,
                IntPtr.Zero))
            {
                throw new NativeException("UpdateProcThreadAttribute");
            }

            attributeList.AddValue(value);
        }

        [DllImport("Kernel32.dll", EntryPoint = "VirtualQueryEx", SetLastError = true)]
        private static extern IntPtr NativeVirtualQueryEx(
            SafeHandle hProcess,
            IntPtr lpAddress,
            ref Helpers.MEMORY_BASIC_INFORMATION lpBuffer,
            IntPtr dwLength);

        public static Helpers.MEMORY_BASIC_INFORMATION VirtualQueryEx(SafeHandle process, IntPtr address)
        {
            Helpers.MEMORY_BASIC_INFORMATION mi = new Helpers.MEMORY_BASIC_INFORMATION();
            if (NativeVirtualQueryEx(process, address, ref mi, (IntPtr)Marshal.SizeOf(mi)) == IntPtr.Zero)
                throw new NativeException("VirtualQueryEx");

            return mi;
        }

        [DllImport("Kernel32.dll", EntryPoint = "WaitForSingleObject")]
        private static extern Helpers.WaitResult NativeWaitForSingleObject(
            SafeHandle hHandle,
            UInt32 dwMilliseconds);

        public static Helpers.WaitResult WaitForSingleObject(SafeHandle handle, UInt32 timeoutMS)
        {
            Helpers.WaitResult res = NativeWaitForSingleObject(handle, timeoutMS);
            if (res == Helpers.WaitResult.WAIT_FAILED)
                throw new NativeException("WaitForSingleObject");

            return res;
        }
    }

    internal class SafeConsoleHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeConsoleHandle() : base(true) { }

        protected override bool ReleaseHandle()
        {
            return Kernel32.ClosePseudoConsole(handle) == 0;
        }
    }

    internal class SafeDuplicateHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private readonly SafeHandle _process;
        private readonly bool _ownsHandle;

        public SafeDuplicateHandle(IntPtr handle, SafeHandle process) : this(handle, process, true) { }

        public SafeDuplicateHandle(IntPtr handle, SafeHandle process, bool ownsHandle) : base(true)
        {
            SetHandle(handle);
            _process = process;
            _ownsHandle = ownsHandle;
        }

        protected override bool ReleaseHandle()
        {
            if (_ownsHandle)
            {
                // Cannot pass this as the handle to close as it appears as closed/invalid already. Just wrap it in
                // a temp SafeHandle that is set not to dispose itself once done.
                Kernel32.DuplicateHandle(_process, new SafeNativeHandle(handle, false), null, 0, false,
                    Helpers.DuplicateHandleOptions.DUPLICATE_CLOSE_SOURCE, false);
            }
            return true;
        }
    }

    internal class SafeNativeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeNativeHandle() : base(true) { }
        public SafeNativeHandle(IntPtr handle) : this(handle, true) { }

        public SafeNativeHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            return Kernel32.CloseHandle(handle);
        }
    }

    internal class SafeProcThreadAttribute : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal List<SafeHandle> values = new List<SafeHandle>();

        public SafeProcThreadAttribute() : base(true) { }
        public SafeProcThreadAttribute(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        public void AddValue(SafeHandle value)
        {
            values.Add(value);
        }

        protected override bool ReleaseHandle()
        {
            foreach (SafeHandle val in values)
                val.Dispose();

            Kernel32.DeleteProcThreadAttributeList(handle);
            Marshal.FreeHGlobal(handle);

            return true;
        }
    }
}
