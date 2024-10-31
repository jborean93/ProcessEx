using ProcessEx.Native;
using ProcessEx.Security;
using System;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace ProcessEx.Commands
{
    [Cmdlet(
        VerbsCommon.Copy, "HandleToProcess",
        SupportsShouldProcess = true
    )]
    [OutputType(typeof(SafeHandle))]
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

        [Parameter()]
        public SwitchParameter OwnHandle { get; set; }

        protected override void ProcessRecord()
        {
            Helpers.DuplicateHandleOptions options = Helpers.DuplicateHandleOptions.NONE;
            if (Access == 0)
                options |= Helpers.DuplicateHandleOptions.DUPLICATE_SAME_ACCESS;

            SafeHandle proc;
            try
            {
                proc = Process.ProcessHandle ?? Kernel32.OpenProcess(Process.ProcessId,
                    ProcessAccessRights.DupHandle, false);
            }
            catch (NativeException e)
            {
                WriteError(ErrorHelper.GenerateWin32Error(e, "Failed to open process to duplicate handle",
                    Process.ProcessId));
                return;
            }

            using SafeHandle currentProc = Kernel32.GetCurrentProcess();
            foreach (SafeHandle h in Handle)
            {
                try
                {
                    WriteObject(Kernel32.DuplicateHandle(currentProc, h, proc, (UInt32)Access, Inherit, options,
                        OwnHandle));
                }
                catch (NativeException e)
                {
                    WriteError(ErrorHelper.GenerateWin32Error(e, "Failed to duplicate handle", Process.ProcessId));
                }
            }
        }
    }
}
