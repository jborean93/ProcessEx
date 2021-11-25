using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace ProcessEx.Native
{
    internal partial class Helpers
    {
        [Flags]
        public enum OPEN_DESKTOP_FLAGS : uint
        {
            NONE = 0x0000,
            DF_ALLOWOTHERACCOUNTHOOK = 0x0001,
        }

        [Flags]
        public enum UserObjectInfoIndex
        {
            UOI_FLAGS = 1,
            UOI_NAME = 2,
            UOI_TYPE = 3,
            UOI_USER_SID = 4,
            UOI_HEAPSIZE = 5,
            UOI_IO = 6,
        }
    }
    internal static class User32
    {
        [DllImport("User32.dll", SetLastError = true)]
        public static extern bool CloseDesktop(
            IntPtr hDesktop);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern bool CloseWindowStation(
            IntPtr hWinSta);

        [DllImport("User32.dll", EntryPoint = "GetProcessWindowStation", SetLastError = true)]
        private static extern IntPtr NativeGetProcessWindowStation();

        public static SafeWindowStation GetProcessWindowStation()
        {
            IntPtr stationHandle = NativeGetProcessWindowStation();
            if (stationHandle == IntPtr.Zero)
                throw new NativeException("GetProcessWindowStation");

            return new SafeWindowStation(stationHandle, false);
        }

        [DllImport("User32.dll", EntryPoint = "GetThreadDesktop", SetLastError = true)]
        private static extern IntPtr NativeGetThreadDesktop(
            Int32 dwThreadId);

        public static SafeDesktop GetThreadDesktop(int threadId)
        {
            IntPtr desktopHandle = NativeGetThreadDesktop(threadId);
            if (desktopHandle == IntPtr.Zero)
                throw new NativeException("GetThreadDesktop");

            return new SafeDesktop(desktopHandle, false);
        }

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool GetUserObjectInformationW(
            SafeHandle hObj,
            Helpers.UserObjectInfoIndex nIndex,
            IntPtr pvInfo,
            Int32 nLength,
            out Int32 lpnLengthNeeded);

        public static SafeMemoryBuffer GetUserObjectInformation(SafeHandle obj, Helpers.UserObjectInfoIndex index)
        {
            GetUserObjectInformationW(obj, index, IntPtr.Zero, 0, out var bytesNeeded);

            SafeMemoryBuffer buffer = new SafeMemoryBuffer(bytesNeeded);
            if (!GetUserObjectInformationW(obj, index, buffer.DangerousGetHandle(),
                bytesNeeded, out _))
            {
                throw new NativeException("GetUserObjectInformation");
            }

            return buffer;
        }

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeDesktop OpenDesktopW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpszDesktop,
            Helpers.OPEN_DESKTOP_FLAGS dwFlags,
            bool fInherit,
            Security.DesktopAccessRights dwDesiredAccess);

        public static SafeDesktop OpenDesktop(string desktop, Helpers.OPEN_DESKTOP_FLAGS flags, bool inherit,
            Security.DesktopAccessRights access)
        {
            SafeDesktop handle = OpenDesktopW(desktop, flags, inherit, access);
            if (handle.DangerousGetHandle() == IntPtr.Zero)
                throw new NativeException("OpenDesktop");

            return handle;
        }

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeWindowStation OpenWindowStationW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpszWinSta,
            bool fInherit,
            Security.StationAccessRights dwDesiredAccess);

        public static SafeWindowStation OpenWindowStation(string station, bool inherit,
            Security.StationAccessRights access)
        {
            SafeWindowStation handle = OpenWindowStationW(station, inherit, access);
            if (handle.DangerousGetHandle() == IntPtr.Zero)
                throw new NativeException("OpenWindowStation");

            return handle;
        }

        [DllImport("User32.dll", EntryPoint = "SetProcessWindowStation", SetLastError = true)]
        private static extern bool NativeSetProcessWindowStation(
            SafeHandle hWinSta);

        public static void SetProcessWindowStation(SafeHandle station)
        {
            if (!NativeSetProcessWindowStation(station))
                throw new NativeException("SetProcessWindowStation");
        }
    }

    internal class SafeDesktop : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeDesktop() : base(true) { }

        public SafeDesktop(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        protected override bool ReleaseHandle()
        {
            return User32.CloseDesktop(handle);
        }
    }

    internal class SafeWindowStation : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeWindowStation() : base(true) { }

        public SafeWindowStation(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        protected override bool ReleaseHandle()
        {
            return User32.CloseWindowStation(handle);
        }
    }
}
