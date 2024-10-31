using ProcessEx.Native;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ProcessEx.Commands
{
    [Cmdlet(
        VerbsCommon.Get, "TokenEnvironment"
    )]
    [OutputType(typeof(Dictionary<string, string>))]
    public class GetTokenEnvironment: PSCmdlet
    {
        [Parameter(
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true
        )]
        public SafeHandle[] Token { get; set; } = Array.Empty<SafeHandle>();

        protected override void ProcessRecord()
        {
            if (Token.Length == 0)
            {
                using SafeHandle token = Advapi32.OpenProcessToken(Kernel32.GetCurrentProcess(),
                    TokenAccessLevels.Query);
                WriteObject(GetEnvironment(token));
            }
            else
            {
                foreach (SafeHandle t in Token)
                {
                    try
                    {
                        WriteObject(GetEnvironment(t));
                    }
                    catch (NativeException e)
                    {
                        WriteError(ErrorHelper.GenerateWin32Error(e, "Failed to get token environment block"));
                    }
                }
            }
        }

        private Dictionary<string, string> GetEnvironment(SafeHandle token)
        {
            using SafeHandle envBlock = Userenv.CreateEnvironmentBlock(token, false);
            return ProcessRunner.ConvertEnvironmentBlock(envBlock);
        }
    }
}
