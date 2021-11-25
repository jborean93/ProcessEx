using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace ProcessEx.Commands
{
    [Cmdlet(
        VerbsCommon.Copy, "HandleToProcess",
        SupportsShouldProcess = true
    )]
    [OutputType(typeof(Native.SafeDuplicateHandle))]
    public class CopyHandleToProcess : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true
        )]
        [Alias("InputObject")]
        public SafeHandle[] Handle = Array.Empty<SafeHandle>();

        [Parameter(
            Mandatory = true,
            Position = 1
        )]
        [ArgumentCompleter(typeof(ProcessCompletor))]
        public ProcessIntString Process { get; set; } = null!;

        [Parameter(
            Position = 2
        )]
        public Int32 Access { get; set; }

        [Parameter()]
        public SwitchParameter Inherit { get; set; }

        protected override void ProcessRecord()
        {
            Debug.Assert(Process != null);
            Process targetProcess = Process.Value;

            Native.Helpers.DuplicateHandleOptions options = Native.Helpers.DuplicateHandleOptions.NONE;
            if (Access == 0)
                options |= Native.Helpers.DuplicateHandleOptions.DUPLICATE_SAME_ACCESS;

            Native.SafeNativeHandle proc;
            try
            {
                proc = Native.Kernel32.OpenProcess(targetProcess.Id,
                    Security.ProcessAccessRights.DupHandle, false);
            }
            catch (NativeException e)
            {
                WriteError(ErrorHelper.GenerateWin32Error(e, "Failed to open process to duplicate handle",
                    targetProcess.Id));
                return;
            }

            using (Native.SafeNativeHandle currentProc = Native.Kernel32.GetCurrentProcess())
            using (proc)
            {
                foreach (SafeHandle h in Handle)
                {
                    try
                    {
                        Native.Kernel32.DuplicateHandle(currentProc, h, proc, (UInt32)Access, Inherit, options);
                    }
                    catch (NativeException e)
                    {
                        WriteError(ErrorHelper.GenerateWin32Error(e, "Failed to duplicate handle", targetProcess.Id));
                    }
                }
            }
        }
    }
}
