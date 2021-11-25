using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace ProcessEx.Native
{
    internal static class Userenv
    {
        [DllImport("Userenv.dll", EntryPoint = "CreateEnvironmentBlock", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool NativeCreateEnvironmentBlock(
            out SafeEnvironmentBlock lpEnvironment,
            SafeHandle hToken,
            bool bInherit);

        public static SafeEnvironmentBlock CreateEnvironmentBlock(SafeHandle token, bool inherit)
        {
            if (!NativeCreateEnvironmentBlock(out var block, token, inherit))
                throw new NativeException("CreateEnvironmentBlock");

            return block;
        }

        [DllImport("Userenv.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool DestroyEnvironmentBlock(
            IntPtr lpEnvironment);
    }

    internal class SafeEnvironmentBlock : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeEnvironmentBlock() : base(true) { }

        protected override bool ReleaseHandle()
        {
            return Userenv.DestroyEnvironmentBlock(handle);
        }
    }
}
