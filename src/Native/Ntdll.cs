using System;
using System.Runtime.InteropServices;

namespace ProcessEx.Native
{
    internal partial class Helpers
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct PEB
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public byte[] Reserved1;
            public byte BeingDebugged;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] public byte[] Reserved2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public IntPtr[] Reserved3;
            public IntPtr Ldr;
            public IntPtr ProcessParameters;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public IntPtr[] Reserved4;
            public IntPtr AltThunkSListPtr;
            public IntPtr Reserved5;
            public UInt32 Reserved6;
            public IntPtr Reserved7;
            public UInt32 Reserved8;
            public UInt32 AltThunkSListPtr32;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 45)] public IntPtr[] Reserved9;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)] public byte[] Reserved10;
            public IntPtr PostProcessInitRoutine;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public byte[] Reserved11;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] public IntPtr[] Reserved12;
            public UInt32 SessionId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PEB_32
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public byte[] Reserved1;
            public byte BeingDebugged;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] public byte[] Reserved2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public Int32[] Reserved3;
            public Int32 Ldr;
            public Int32 ProcessParameters;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public Int32[] Reserved4;
            public Int32 AltThunkSListPtr;
            public Int32 Reserved5;
            public UInt32 Reserved6;
            public Int32 Reserved7;
            public UInt32 Reserved8;
            public UInt32 AltThunkSListPtr32;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 45)] public Int32[] Reserved9;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)] public byte[] Reserved10;
            public Int32 PostProcessInitRoutine;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public byte[] Reserved11;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] public Int32[] Reserved12;
            public UInt32 SessionId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PEB_64
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public byte[] Reserved1;
            public byte BeingDebugged;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] public byte[] Reserved2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public Int64[] Reserved3;
            public Int64 Ldr;
            public Int64 ProcessParameters;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public Int64[] Reserved4;
            public Int64 AltThunkSListPtr;
            public Int64 Reserved5;
            public UInt32 Reserved6;
            public Int64 Reserved7;
            public UInt32 Reserved8;
            public UInt32 AltThunkSListPtr32;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 45)] public Int64[] Reserved9;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)] public byte[] Reserved10;
            public Int64 PostProcessInitRoutine;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public byte[] Reserved11;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] public Int64[] Reserved12;
            public UInt32 SessionId;
        }

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
        public struct PROCESS_BASIC_INFORMATION_64
        {
            public UInt32 ExitStatus;
            public Int64 PebBaseAddress;
            public UInt64 AffinityMask;
            public UInt32 BasePriority;
            public UInt64 UniqueProcessId;
            public UInt64 InheritedFromUniqueProcessId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PUBLIC_OBJECT_BASIC_INFORMATION
        {
            public UInt32 Attributes;
            public UInt32 AccessMask;
            public UInt32 HandleCount;
            public UInt32 PointerCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)] public UInt32[] Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RTL_USER_PROCESS_PARAMETERS
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] Reserved1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)] public IntPtr[] Reserved2;
            public UNICODE_STRING ImagePathName;
            public UNICODE_STRING CommandLine;
            public IntPtr Environment; // Undocumented but comes after CommandLine
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RTL_USER_PROCESS_PARAMETERS_32
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] Reserved1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)] public Int32[] Reserved2;
            public UNICODE_STRING_32 ImagePathName;
            public UNICODE_STRING_32 CommandLine;
            public Int32 Environment; // Undocumented but comes after CommandLine
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RTL_USER_PROCESS_PARAMETERS_64
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] Reserved1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)] public Int64[] Reserved2;
            public UNICODE_STRING_64 ImagePathName;
            public UNICODE_STRING_64 CommandLine;
            public Int64 Environment; // Undocumented but comes after CommandLine
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            public UInt16 Length;
            public UInt16 MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING_32
        {
            public UInt16 Length;
            public UInt16 MaximumLength;
            public Int32 Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING_64
        {
            public UInt16 Length;
            public UInt16 MaximumLength;
            public Int64 Buffer;
        }

        public enum OBJECT_INFORMATION_CLASS
        {
            ObjectBasicInformation = 0,
            ObjectNameInformation = 1,
            ObjectTypeInformation = 2,
            ObjectAllTypesInformation = 3,
            ObjectDataInformation = 4,
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
            SafeHandle ProcessInformation,
            Int32 ProcessInformationLength,
            out IntPtr ReturnLength);

        public static SafeMemoryBuffer NtQueryInformationProcess(SafeHandle process,
            Helpers.ProcessInformationClass infoClass)
        {
            int bufferLength;
            if (infoClass == Helpers.ProcessInformationClass.ProcessBasicInformation)
                bufferLength = Marshal.SizeOf<Helpers.PROCESS_BASIC_INFORMATION>();
            else if (infoClass == Helpers.ProcessInformationClass.ProcessWow64Information)
                bufferLength = IntPtr.Size;
            else
                throw new NotImplementedException(infoClass.ToString());

            SafeMemoryBuffer buffer = new SafeMemoryBuffer(bufferLength);
            UInt32 res = NativeNtQueryInformationProcess(process, infoClass, buffer, buffer.Length,
                out var returnLength);

            if (res != 0)
            {
                buffer.Dispose();
                throw new NativeException("NtQueryInformationProcess", RtlNtStatusToDosError(res));
            }

            buffer.Length = (int)returnLength;
            return buffer;
        }

        [DllImport("Ntdll.dll", EntryPoint = "NtQueryObject")]
        private static extern UInt32 NativeNtQueryObject(
            SafeHandle Handle,
            Helpers.OBJECT_INFORMATION_CLASS ObjectInformationClass,
            SafeHandle ObjectInformation,
            Int32 ObjectInformationLength,
            out Int32 ReturnLength);

        public static SafeMemoryBuffer NtQueryObject(SafeHandle handle, Helpers.OBJECT_INFORMATION_CLASS infoClass)
        {
            int bufferLength;
            if (infoClass == Helpers.OBJECT_INFORMATION_CLASS.ObjectBasicInformation)
                bufferLength = Marshal.SizeOf<Helpers.PUBLIC_OBJECT_BASIC_INFORMATION>();
            else
                throw new NotImplementedException(infoClass.ToString());

            SafeMemoryBuffer buffer = new SafeMemoryBuffer(bufferLength);
            UInt32 res = NativeNtQueryObject(handle, infoClass, buffer, buffer.Length, out var returnLength);

            if (res != 0)
            {
                buffer.Dispose();
                throw new NativeException("NtQueryObject", RtlNtStatusToDosError(res));
            }

            buffer.Length = (int)returnLength;
            return buffer;
        }

        [DllImport("Ntdll.dll", EntryPoint = "NtWow64QueryInformationProcess64")]
        private static extern UInt32 NativeNtWow64QueryInformationProcess64(
            SafeHandle hProcess,
            Helpers.ProcessInformationClass processInformationClass,
            SafeHandle ProcessInformation,
            Int32 ProcessInformationLength,
            out IntPtr ReturnLength);

        public static SafeMemoryBuffer NtWow64QueryInformationProcess64(SafeHandle process,
            Helpers.ProcessInformationClass infoClass)
        {
            int bufferLength;
            if (infoClass == Helpers.ProcessInformationClass.ProcessBasicInformation)
                bufferLength = Marshal.SizeOf<Helpers.PROCESS_BASIC_INFORMATION_64>();
            else if (infoClass == Helpers.ProcessInformationClass.ProcessWow64Information)
                bufferLength = 8;
            else
                throw new NotImplementedException(infoClass.ToString());

            SafeMemoryBuffer buffer = new SafeMemoryBuffer(bufferLength);
            UInt32 res = NativeNtWow64QueryInformationProcess64(process, infoClass, buffer, buffer.Length,
                out var returnLength);

            if (res != 0)
            {
                buffer.Dispose();
                throw new NativeException("NtQueryInformationProcess", RtlNtStatusToDosError(res));
            }

            buffer.Length = (int)returnLength;
            return buffer;
        }

        [DllImport("Ntdll.dll", EntryPoint = "NtWow64ReadVirtualMemory64")]
        private static extern UInt32 NativeNtWow64ReadVirtualMemory64(
            SafeHandle hProcess,
            Int64 BaseAddress,
            SafeHandle Buffer,
            Int64 BufferLength,
            out Int64 ReturnLength);

        public static SafeMemoryBuffer NtWow64ReadVirtualMemory64(SafeHandle process, Int64 address, Int32 length)
        {
            SafeMemoryBuffer buffer = new SafeMemoryBuffer(length);
            UInt32 res = NativeNtWow64ReadVirtualMemory64(process, address, buffer, length, out var read);
            if (res != 0)
            {
                buffer.Dispose();
                throw new NativeException("ReadProcessMemory", RtlNtStatusToDosError(res));
            }

            buffer.Length = (int)read;
            return buffer;
        }

        [DllImport("Ntdll.dll")]
        public static extern Int32 RtlNtStatusToDosError(
            UInt32 Status);
    }
}
