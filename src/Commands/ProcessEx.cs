using ProcessEx.Native;
using ProcessEx.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace ProcessEx.Commands
{
    [Cmdlet(
        VerbsCommon.Get, "ProcessEx"
    )]
    [OutputType(typeof(ProcessInfo))]
    public class GetProcessEx : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true
        )]
        [Alias("Id")]
        [ArgumentCompleter(typeof(ProcessCompletor))]
        public ProcessIntString[] Process { get; set; } = Array.Empty<ProcessIntString>();

        [Parameter(
            Position = 1
        )]
        public ProcessAccessRights Access { get; set; } = ProcessAccessRights.AllAccess;

        [Parameter()]
        public SwitchParameter Inherit { get; set; }

        protected override void ProcessRecord()
        {
            foreach (ProcessIntString proc in Process)
            {
                SafeHandle processHandle;
                try
                {
                    processHandle = Kernel32.OpenProcess(proc.Value.Id, Access, Inherit);
                }
                catch (NativeException e)
                {
                    WriteError(ErrorHelper.GenerateWin32Error(e, "Failed to open process handle", proc.Value.Id));
                    continue;
                }

                SafeHandle threadHandle = Helpers.NULL_HANDLE_VALUE; // We cannot get the main TID after creation.
                WriteObject(new ProcessInfo(processHandle, threadHandle, proc.Value.Id, 0));
            }
        }
    }

    [Cmdlet(
        VerbsLifecycle.Start, "ProcessEx",
        DefaultParameterSetName = "FilePath"
    )]
    [OutputType(null, typeof(ProcessInfo))]
    public class StartProcessEx : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ParameterSetName = "FilePath"
        )]
        public string FilePath { get; set; } = "";

        [Parameter(
            ValueFromRemainingArguments = true,
            ParameterSetName = "FilePath"
        )]
        public string[] ArgumentList { get; set; } = Array.Empty<string>();

        [Parameter(
            Mandatory = true,
            ParameterSetName = "CommandLine"
        )]
        public string CommandLine { get; set; } = "";

        [Parameter(
            ParameterSetName = "CommandLine"
        )]
        public string ApplicationName { get; set; } = "";

        [Parameter()]
        public string WorkingDirectory { get; set; } = "";

        [Parameter()]
        public StartupInfo? StartupInfo { get; set; }

        [Parameter()]
        public CreationFlags CreationFlags { get; set; } = CreationFlags.NewConsole;

        [Parameter()]
        public SecurityAttributes? ProcessAttribute { get; set; }

        [Parameter()]
        public SecurityAttributes? ThreadAttribute { get; set; }

        [Parameter()]
        public IDictionary? Environment { get; set; }

        [Parameter()]
        public SafeHandle? Token { get; set; }

        [Parameter()]
        public SwitchParameter UseNewEnvironment { get; set; }

        [Parameter()]
        public SwitchParameter DisableInheritance { get; set; }

        [Parameter()]
        public SwitchParameter Wait { get; set; }

        [Parameter()]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            if (ParameterSetName == "FilePath")
            {
                ApplicationName = ArgumentHelper.ResolveExecutable(this, FilePath);
                List<string> commands = new List<string>() { ApplicationName };
                commands.AddRange(ArgumentList);
                CommandLine = String.Join(" ", commands.Select(a => ArgumentHelper.EscapeArgument(a)));
            }

            if (StartupInfo == null)
                StartupInfo = new StartupInfo();

            if ((StartupInfo?.ConPTY.DangerousGetHandle() != IntPtr.Zero) && CreationFlags == CreationFlags.NewConsole)
            {
                CreationFlags = CreationFlags.None;
            }

            // Always start suspended so the process object can be built properly. Just determine whether to continue
            // waiting if the user desired that.
            bool suspended = (CreationFlags & CreationFlags.Suspended) == CreationFlags.Suspended;
            CreationFlags |= CreationFlags.Suspended;

            if (Wait && suspended)
            {
                ArgumentException ex = new ArgumentException("Cannot use -Wait with -CreationFlags Suspended");
                WriteError(new ErrorRecord(ex, "SuspendWithWait", ErrorCategory.InvalidArgument, null));
                return;
            }
            if (Environment?.Count > 0 && UseNewEnvironment)
            {
                ArgumentException ex = new ArgumentException("Cannot use -UseNewEnvironment with environment vars");
                WriteError(new ErrorRecord(ex, "UseNewEnvironmentWithEnvironment", ErrorCategory.InvalidArgument,
                    null));
                return;
            }
            if (DisableInheritance && StartupInfo?.InheritedHandles.Length > 0)
            {
                ArgumentException ex = new ArgumentException(
                    "Cannot -DisableInheritance with explicit inherited handles in StartupInfo");
                WriteError(new ErrorRecord(ex, "DisableInheritedWithInheritedHandles", ErrorCategory.InvalidArgument,
                    null));
                return;
            }

            WriteVerbose(String.Format("Starting new process with\n\t{0}", String.Join("\n\t", new string[]
            {
                $"ApplicationName: {ApplicationName}",
                $"CommandLine: {CommandLine}",
                $"DisableInheritance: {DisableInheritance}",
                $"CreationFlags: {CreationFlags}",
                $"WorkingDirectory: {WorkingDirectory}",
            })));

            ProcessInfo info;
            if (Token != null)
            {
                info = ProcessRunner.CreateProcessAsUser(Token, ApplicationName, CommandLine, ProcessAttribute,
                    ThreadAttribute, DisableInheritance, CreationFlags, Environment, WorkingDirectory, StartupInfo!,
                    UseNewEnvironment);
            }
            else
            {
                info = ProcessRunner.CreateProcess(ApplicationName, CommandLine, ProcessAttribute, ThreadAttribute,
                    DisableInheritance, CreationFlags, Environment, WorkingDirectory, StartupInfo!, UseNewEnvironment);
            }

            WriteVerbose($"Process created with PID {info.ProcessId} and TID {info.ThreadId}");
            if (Wait)
            {
                WriteVerbose("Resuming process and waiting for it to complete");
                ProcessRunner.ResumeAndWait(info);
            }
            else if (!suspended)
            {
                WriteVerbose("Resuming process");
                Kernel32.ResumeThread(info.Thread);
            }

            if (PassThru)
                WriteObject(info);
        }
    }
}
