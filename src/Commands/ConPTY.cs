using System;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace ProcessEx.Commands
{
    [Cmdlet(
        VerbsCommon.New, "ConPTY"
    )]
    [OutputType(typeof(Native.SafeConsoleHandle))]
    public class NewConPTY : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true
        )]
        [Alias("X")]
        public Int16 Width;

        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipelineByPropertyName = true
        )]
        [Alias("Y")]
        public Int16 Height;

        [Parameter(
            Mandatory = true,
            Position = 2,
            ValueFromPipelineByPropertyName = true
        )]
        public SafeHandle InputPipe = Native.Helpers.NULL_HANDLE_VALUE;

        [Parameter(
            Mandatory = true,
            Position = 3,
            ValueFromPipelineByPropertyName = true
        )]
        public SafeHandle OutputPipe = Native.Helpers.NULL_HANDLE_VALUE;

        [Parameter()]
        public SwitchParameter InheritCursor;

        protected override void ProcessRecord()
        {
            Native.Helpers.COORD size = new Native.Helpers.COORD()
            {
                X = Width,
                Y = Height,
            };
            Native.Helpers.PseudoConsoleCreateFlags flags = Native.Helpers.PseudoConsoleCreateFlags.NONE;
            if (InheritCursor)
                flags |= Native.Helpers.PseudoConsoleCreateFlags.PSEUDOCONSOLE_INHERIT_CURSOR;


            try
            {
                WriteObject(Native.Kernel32.CreatePseudoConsole(size, InputPipe, OutputPipe, flags));
            }
            catch (NativeException e)
            {
                WriteError(ErrorHelper.GenerateWin32Error(e, "Failed to create psuedo console handle"));
            }
        }
    }

    [Cmdlet(
        VerbsCommon.Resize, "ConPTY"
    )]
    public class ResizeConPTY : PSCmdlet
    {
        private Native.Helpers.COORD _size = new Native.Helpers.COORD();

        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true
        )]
        [Alias("InputObject")]
        public SafeHandle[] ConPTY = Array.Empty<SafeHandle>();

        [Parameter(
            Mandatory = true,
            Position = 1
        )]
        [Alias("X")]
        public Int16 Width;

        [Parameter(
            Mandatory = true,
            Position = 2
        )]
        [Alias("Y")]
        public Int16 Height;

        protected override void BeginProcessing()
        {
            _size.X = Width;
            _size.Y = Height;
        }

        protected override void ProcessRecord()
        {
            foreach (SafeHandle pty in ConPTY)
            {
                try
                {
                    Native.Kernel32.ResizePseudoConsole(pty, _size);
                }
                catch (NativeException e)
                {
                    WriteError(ErrorHelper.GenerateWin32Error(e, "Failed to resize psuedo console handle"));
                }
            }
        }
    }
}
