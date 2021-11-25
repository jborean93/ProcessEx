using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace ProcessEx
{
    internal class SafeMemoryBuffer : SafeHandleZeroOrMinusOneIsInvalid
    {
        public int Length { get; internal set; } = 0;

        public SafeMemoryBuffer() : base(true) { }
        public SafeMemoryBuffer(int cb) : base(true)
        {
            base.SetHandle(Marshal.AllocHGlobal(cb));
            Length = cb;
        }
        public SafeMemoryBuffer(IntPtr handle, int length) : base(true)
        {
            base.SetHandle(handle);
            Length = length;
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }
}
