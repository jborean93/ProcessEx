using ProcessEx.Native;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security;

namespace ProcessEx.Commands
{
    [Cmdlet(
        VerbsLifecycle.Start, "ProcessWith",
        DefaultParameterSetName = "FilePathCredential"
    )]
    [OutputType(null, typeof(ProcessInfo))]
    public class StartProcessWith : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ParameterSetName = "FilePathCredential"
        )]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ParameterSetName = "FilePathToken"
        )]
        public string FilePath { get; set; } = "";

        [Parameter(
            ValueFromRemainingArguments = true,
            ParameterSetName = "FilePathCredential"
        )]
        [Parameter(
            ValueFromRemainingArguments = true,
            ParameterSetName = "FilePathToken"
        )]
        public string[] ArgumentList { get; set; } = Array.Empty<string>();

        [Parameter(
            Mandatory = true,
            ParameterSetName = "CommandLineCredential"
        )]
        [Parameter(
            Mandatory = true,
            ParameterSetName = "CommandLineToken"
        )]
        public string CommandLine { get; set; } = "";

        [Parameter(
            ParameterSetName = "CommandLineCredential"
        )]
        [Parameter(
            ParameterSetName = "CommandLineToken"
        )]
        public string ApplicationName { get; set; } = "";

        [Parameter(
            Mandatory = true,
            ParameterSetName = "FilePathCredential"
        )]
        [Parameter(
            Mandatory = true,
            ParameterSetName = "CommandLineCredential"
        )]
        [ValidateNotNull]
        public PSCredential Credential { get; set; } = new PSCredential("dummy", new SecureString());

        [Parameter(
            Mandatory = true,
            ParameterSetName = "FilePathToken"
        )]
        [Parameter(
            Mandatory = true,
            ParameterSetName = "CommandLineToken"
        )]
        public SafeHandle? Token { get; set; }

        [Parameter()]
        public string WorkingDirectory { get; set; } = "";

        [Parameter()]
        public StartupInfo? StartupInfo { get; set; }

        [Parameter()]
        public CreationFlags CreationFlags { get; set; } = CreationFlags.NewConsole |
            CreationFlags.DefaultErrorMode | CreationFlags.NewProcessGroup;

        [Parameter()]
        public IDictionary? Environment { get; set; }

        [Parameter()]
        public SwitchParameter WithProfile { get; set; }

        [Parameter()]
        public SwitchParameter NetCredentialsOnly { get; set; }

        [Parameter()]
        public SwitchParameter Wait { get; set; }

        [Parameter()]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            if (ParameterSetName == "FilePathCredential" || ParameterSetName == "FilePathToken")
            {
                try
                {
                    ApplicationName = ArgumentHelper.ResolveExecutable(this, FilePath);
                }
                catch (NullReferenceException) // No documentation on this exception.
                {
                    ApplicationName = FilePath;
                }

                List<string> commands = new List<string>() { ApplicationName };
                commands.AddRange(ArgumentList);
                CommandLine = String.Join(" ", commands.Select(a => ArgumentHelper.EscapeArgument(a)));
            }

            if (String.IsNullOrWhiteSpace(WorkingDirectory))
                WorkingDirectory = SessionState.Path.CurrentFileSystemLocation.Path;

            if (StartupInfo == null)
                StartupInfo = new StartupInfo();

            if (StartupInfo.InheritedHandles.Length > 0)
            {
                ArgumentException ex = new ArgumentException("Start-ProcessWith cannot be used with InheritedHandles");
                WriteError(new ErrorRecord(ex, "WithAndInheritedHandles", ErrorCategory.InvalidArgument, null));
                return;
            }
            if (StartupInfo.ParentProcess != 0)
            {
                ArgumentException ex = new ArgumentException("Start-ProcessWith cannot be used with ParentProcess");
                WriteError(new ErrorRecord(ex, "WithAndParentProcess", ErrorCategory.InvalidArgument, null));
                return;
            }
            if (StartupInfo.ConPTY.DangerousGetHandle() != IntPtr.Zero)
            {
                ArgumentException ex = new ArgumentException("Start-ProcessWith cannot be used with ConPTY");
                WriteError(new ErrorRecord(ex, "WithAndConPTY", ErrorCategory.InvalidArgument, null));
                return;
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

            Helpers.LogonFlags logonFlags = Helpers.LogonFlags.NONE;
            if (WithProfile)
                logonFlags |= Helpers.LogonFlags.LOGON_WITH_PROFILE;

            if (NetCredentialsOnly)
                logonFlags |= Helpers.LogonFlags.LOGON_NETCREDENTIALS_ONLY;

            WriteVerbose(String.Format("Starting new process with\n\t{0}", String.Join("\n\t", new string[]
            {
                $"ApplicationName: {ApplicationName}",
                $"CommandLine: {CommandLine}",
                $"CreationFlags: {CreationFlags}",
                $"WorkingDirectory: {WorkingDirectory}",
                $"LogonFlags: {logonFlags}",
            })));

            ProcessInfo info;
            try
            {
                if (Token != null)
                {
                    info = ProcessRunner.CreateProcessWithToken(Token, logonFlags, ApplicationName, CommandLine,
                        CreationFlags, Environment, WorkingDirectory, StartupInfo!);
                }
                else
                {
                    string username = Credential.UserName;
                    string? domain = null;
                    if (username.Contains("\\"))
                    {
                        string[] userSplit = username.Split(new char[1] { '\\' }, 2);
                        domain = userSplit[0];
                        username = userSplit[1];
                    }

                    info = ProcessRunner.CreateProcessWithLogon(username, domain, Credential.Password, logonFlags,
                        ApplicationName, CommandLine, CreationFlags, Environment, WorkingDirectory, StartupInfo!);
                }
            }
            catch (NativeException e)
            {
                WriteError(ErrorHelper.GenerateWin32Error(e, "Failed to create process"));
                return;
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
                ProcessRunner.ResumeThread(info.Thread);
            }

            if (PassThru)
                WriteObject(info);
        }
    }
}
