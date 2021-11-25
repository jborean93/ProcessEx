using System;
using System.Runtime.InteropServices;

namespace ProcessEx.Native
{
    internal partial class Helpers
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_BASIC_INFORMATION
        {
            public UInt32 ExitStatus;
            public IntPtr PebBaseAddress;
            public UIntPtr AffinityMask;
            public UInt32 BasePriority;
            public UIntPtr UniqueProcessId;
            public UIntPtr InheritedFromUniqueProcessId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RTL_USER_PROCESS_PARAMETERS
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] Reserved1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)] public IntPtr[] Reserved2;
            public UNICODE_STRING ImagePathName;
            public UNICODE_STRING CommandLine;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            public UInt16 Length;
            public UInt16 MaximumLength;
            public IntPtr Buffer;
        }

        public enum ProcessInformationClass
        {
            ProcessBasicInformation = 0,
            ProcessDebugPort = 7,
            ProcessWow64Information = 26,
            ProcessImageFileName = 27,
            ProcessBreakOnTermination = 29,
            ProcessSubsystemInformation = 75,
        }
    }

    internal static class Ntdll
    {
        [DllImport("Ntdll.dll", EntryPoint = "NtQueryInformationProcess")]
        private static extern UInt32 NativeNtQueryInformationProcess(
            SafeHandle ProcessHandle,
            Helpers.ProcessInformationClass processInformationClass,
            IntPtr ProcessInformation,
            Int32 ProcessInformationLength,
            out IntPtr ReturnLength);

        public static SafeMemoryBuffer NtQueryInformationProcess(SafeHandle process,
            Helpers.ProcessInformationClass infoClass)
        {
            if (infoClass != Helpers.ProcessInformationClass.ProcessBasicInformation)
                throw new NotImplementedException(); // Thise code only needs the basic information.

            SafeMemoryBuffer buffer = new SafeMemoryBuffer(Marshal.SizeOf<Helpers.PROCESS_BASIC_INFORMATION>());
            UInt32 res = NativeNtQueryInformationProcess(process, infoClass, buffer.DangerousGetHandle(),
                buffer.Length, out _);

            if (res != 0)
            {
                buffer.Dispose();
                throw new NativeException("NtQueryInformationProcess", RtlNtStatusToDosError(res));
            }

            return buffer;
        }

        [DllImport("Ntdll.dll")]
        public static extern Int32 RtlNtStatusToDosError(
            UInt32 Status);
    }
}
