using ProcessEx.Native;
using ProcessEx.Security;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace ProcessEx.Commands
{
    [Cmdlet(
        VerbsCommon.Get, "ProcessEnvironment"
    )]
    [OutputType(typeof(Dictionary<string, string>))]
    public class GetProcessEnvironment : PSCmdlet
    {
        [Parameter(
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true
        )]
        [Alias("Id")]
        [ArgumentCompleter(typeof(ProcessCompletor))]
        public ProcessIntString[] Process { get; set; } = Array.Empty<ProcessIntString>();

        protected override void ProcessRecord()
        {
            if (Process.Length == 0)
            {
                WriteObject(ProcessInfo.GetCurrentProcess().Environment);
            }
            else
            {
                SafeHandle threadHandle = Helpers.NULL_HANDLE_VALUE; // We cannot get the main TID after creation.

                foreach (ProcessIntString proc in Process)
                {
                    SafeHandle processHandle;
                    try
                    {
                        processHandle = proc.ProcessHandle ?? Kernel32.OpenProcess(proc.ProcessId,
                            ProcessAccessRights.QueryInformation | ProcessAccessRights.VMRead, false);
                    }
                    catch (NativeException e)
                    {
                        WriteError(ErrorHelper.GenerateWin32Error(e, "Failed to open process handle", proc.ProcessId));
                        continue;
                    }

                    using (processHandle)
                        WriteObject(new ProcessInfo(processHandle, threadHandle, proc.ProcessId, 0).Environment);
                }
            }
        }
    }
}
