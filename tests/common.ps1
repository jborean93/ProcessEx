#Requires -Module PSPrivilege

$moduleName = (Get-Item ([IO.Path]::Combine($PSScriptRoot, '..', 'module', '*.psd1'))).BaseName
$manifestPath = [IO.Path]::Combine($PSScriptRoot, '..', 'output', $moduleName)

if (-not (Get-Module -Name $moduleName -ErrorAction SilentlyContinue)) {
    Import-Module $manifestPath
}

# FUTURE: Use PSAccessToken once it's ready
Add-Type -TypeDefinition @'
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace ProcessExTests
{
    public class Helpers
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public UInt32 LowPart;
            public Int32 HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public PrivilegeAttributes Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROFILEINFO
        {
            public int dwSize;
            public int dwFlags;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpUserName;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpProfilePath;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpDefaultPath;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpServerName;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpPolicyPath;
            public IntPtr hProfile;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES
        {
            public UInt32 PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public LUID_AND_ATTRIBUTES[] Privileges;
        }
    }

    public class Native
    {
        [DllImport("User32.dll")]
        public static extern bool CloseDesktop(
            IntPtr hDesktop);

        [DllImport("User32.dll")]
        public static extern bool CloseWindowStation(
            IntPtr hWinSta);

        [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeDesktopHandle CreateDesktopExW(
            string lpszDesktop,
            string lpszDevice,
            IntPtr pDevmode,
            CreateDesktopFlags dwFlags,
            int dwDesiredAccess,
            IntPtr lpsa,
            int ulHeapSize,
            IntPtr pvoid);

        public static SafeDesktopHandle CreateDesktopEx(string name, CreateDesktopFlags flags, int access,
            int heapSizeKb)
        {
            SafeDesktopHandle handle = CreateDesktopExW(name, null, IntPtr.Zero, flags, access, IntPtr.Zero,
                heapSizeKb, IntPtr.Zero);
            if (handle.IsInvalid)
                throw new Win32Exception();

            return handle;
        }

        [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeStationHandle CreateWindowStationW(
            string lpwinsta,
            CreateStationFlags dwFlags,
            int dwDesiredAccess,
            IntPtr lpsa);

        public static SafeStationHandle CreateWindowStation(string name, CreateStationFlags flags, int access)
        {
            SafeStationHandle handle = CreateWindowStationW(name, flags, access, IntPtr.Zero);
            if (handle.IsInvalid)
                throw new Win32Exception();

            return handle;
        }

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeProcessHandle CreateJobObjectW(
            IntPtr lpJobAttributes,
            string lpName);

        public static SafeProcessHandle CreateJobObject(string name)
        {
            SafeProcessHandle job = CreateJobObjectW(IntPtr.Zero, name);
            if (job.DangerousGetHandle() == IntPtr.Zero)
                throw new Win32Exception();

            return job;
        }

        [DllImport("Userenv.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool DeleteProfileW(
            string lpSidString,
            string lpProfilePath,
            string lpComputerName);

        public static void DeleteProfile(string sid, string profilePath, string computerName)
        {
            if (!DeleteProfileW(sid, profilePath, computerName))
                throw new Win32Exception();
        }

        [DllImport("Advapi32.dll", EntryPoint = "DuplicateTokenEx", SetLastError = true)]
        private static extern bool NativeDuplicateTokenEx(
            SafeHandle hExistingToken,
            TokenAccessLevels dwDesiredAccess,
            IntPtr lpTokenAttributes,
            SecurityImpersonationLevel ImpersonationLevel,
            TokenType TokenType,
            out SafeAccessTokenHandle phNewToken);

        public static SafeAccessTokenHandle DuplicateTokenEx(SafeHandle token, TokenAccessLevels access,
            SecurityImpersonationLevel impersonationLevel, TokenType tokenType)
        {
            SafeAccessTokenHandle handle;
            if (!NativeDuplicateTokenEx(token, access, IntPtr.Zero, impersonationLevel, tokenType, out handle))
                throw new Win32Exception();

            return handle;
        }

        [DllImport("Kernel32.dll", EntryPoint = "GetCurrentThread")]
        private static extern IntPtr NativeGetCurrentThread();

        public static SafeProcessHandle GetCurrentThread()
        {
            return new SafeProcessHandle(NativeGetCurrentThread(), false);
        }

        [DllImport("Kernel32.dll")]
        public static extern int GetCurrentThreadId();

        [DllImport("Kernel32.dll", EntryPoint = "GetProcessId", SetLastError = true)]
        private static extern int NativeGetProcessId(SafeHandle Process);

        public static int GetProcessId(SafeHandle process)
        {
            int pid = NativeGetProcessId(process);
            if (pid == 0)
                throw new Win32Exception();

            return pid;
        }

        [DllImport("User32.dll", EntryPoint = "GetProcessWindowStation", SetLastError = true)]
        private static extern IntPtr NativeGetProcessWindowStation();

        public static SafeStationHandle GetProcessWindowStation()
        {
            IntPtr stationHandle = NativeGetProcessWindowStation();
            if (stationHandle == IntPtr.Zero)
                throw new Win32Exception();

            return new SafeStationHandle(stationHandle, false);
        }

        [DllImport("User32.dll", EntryPoint = "GetThreadDesktop", SetLastError = true)]
        private static extern IntPtr NativeGetThreadDesktop(
            int dwThreadId);

        public static SafeDesktopHandle GetThreadDesktop(int threadId)
        {
            IntPtr desktop = NativeGetThreadDesktop(threadId);
            if (desktop == IntPtr.Zero)
                throw new Win32Exception();

            return new SafeDesktopHandle(desktop, false);
        }

        [DllImport("Kernel32.dll", EntryPoint = "GetThreadId", SetLastError = true)]
        private static extern int NativeGetThreadId(SafeHandle Thread);

        public static int GetThreadId(SafeHandle thread)
        {
            int pid = NativeGetThreadId(thread);
            if (pid == 0)
                throw new System.ComponentModel.Win32Exception();

            return pid;
        }

        [DllImport("Advapi32.dll", EntryPoint = "GetTokenInformation", SetLastError = true)]
        private static extern bool NativeGetTokenInformation(
            SafeHandle TokenHandle,
            int TokenInformationClass,
            IntPtr TokenInformation,
            int TokenInformationLength,
            out int ReturnLength);

        public static SafeMemoryBuffer GetTokenInformation(SafeHandle token, int infoClass)
        {
            int returnLength;
            NativeGetTokenInformation(token, infoClass, IntPtr.Zero, 0, out returnLength);

            SafeMemoryBuffer buffer = new SafeMemoryBuffer(returnLength);
            if (!NativeGetTokenInformation(token, infoClass, buffer.DangerousGetHandle(), returnLength,
                out returnLength))
            {
                int errCode = Marshal.GetLastWin32Error();
                buffer.Dispose();
                throw new Win32Exception(errCode);
            }

            return buffer;
        }

        [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetUserObjectInformationW(
            SafeHandle hObj,
            int nIndex,
            IntPtr pvInfo,
            int nLength,
            out int lpnLengthNeeded);

        public static string GetUserObjectName(SafeHandle obj)
        {
            int bytesNeeded;
            GetUserObjectInformationW(obj, 2, IntPtr.Zero, 0, out bytesNeeded);

            IntPtr buffer = Marshal.AllocHGlobal(bytesNeeded);
            if (!GetUserObjectInformationW(obj, 2, buffer, bytesNeeded, out bytesNeeded))
            {
                int errCode = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(errCode);
            }

            try
            {
                return Marshal.PtrToStringUni(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        [DllImport("Userenv.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetUserProfileDirectoryW(
            SafeHandle hToken,
            StringBuilder lpProfileDir,
            ref int lpcchSize);

        public static string GetUserProfileDirectory(SafeHandle token)
        {
            int profileLength = 0;
            GetUserProfileDirectoryW(token, null, ref profileLength);

            StringBuilder profile = new StringBuilder(profileLength);
            if (!GetUserProfileDirectoryW(token, profile, ref profileLength))
                throw new Win32Exception();

            return profile.ToString();
        }

        [DllImport("Advapi32.dll", EntryPoint = "ImpersonateLoggedOnUser", SetLastError = true)]
        private static extern bool NativeImpersonateLoggedOnUser(
            SafeHandle hToken);

        public static void ImpersonateLoggedOnUser(SafeHandle token)
        {
            if (!NativeImpersonateLoggedOnUser(token))
                throw new Win32Exception();
        }

        [DllImport("Kernel32.dll", EntryPoint = "IsProcessInJob", SetLastError = true)]
        private static extern bool NativeIsProcessInJob(
            SafeHandle ProcessHandle,
            SafeHandle JobHandle,
            out bool Result);

        public static bool IsProcessInJob(SafeHandle process, SafeHandle job)
        {
            bool res;
            if (!NativeIsProcessInJob(process, job, out res))
            {
                throw new Win32Exception();
            }

            return res;
        }

        [DllImport("Userenv.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool LoadUserProfileW(
            SafeHandle hToken,
            ref Helpers.PROFILEINFO lpProfileInfo);

        public static Helpers.PROFILEINFO LoadUserProfile(SafeHandle token, string username)
        {
            Helpers.PROFILEINFO pi = new Helpers.PROFILEINFO();
            pi.dwSize = Marshal.SizeOf(pi);
            pi.lpUserName = username;

            if (!LoadUserProfileW(token, ref pi))
                throw new Win32Exception();

            return pi;
        }

        [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool LogonUserW(
            string lpszUsername,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            out SafeAccessTokenHandle phToken);

        public static SafeAccessTokenHandle LogonUser(string username, string domain, string password, int logonType,
            int logonProvider)
        {
            SafeAccessTokenHandle token;
            if (!LogonUserW(username, domain, password, logonType, logonProvider, out token))
                throw new Win32Exception();

            return token;
        }

        [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool LookupPrivilegeNameW(
            string lpSystemName,
            ref Helpers.LUID lpLuid,
            StringBuilder lpName,
            ref int cchName);

        public static string LookupPrivilegeName(string system, Helpers.LUID luid)
        {
            int nameLen = 0;
            LookupPrivilegeNameW(system, ref luid, null, ref nameLen);

            StringBuilder name = new StringBuilder(nameLen + 1);
            if (!LookupPrivilegeNameW(system, ref luid, name, ref nameLen))
                throw new Win32Exception();

            return name.ToString();
        }

        [DllImport("Advapi32.dll", EntryPoint = "OpenProcessToken", SetLastError = true)]
        private static extern bool NativeOpenProcessToken(
            SafeHandle ProcessHandle,
            int DesiredAccess,
            out SafeAccessTokenHandle TokenHandle);

        public static SafeAccessTokenHandle OpenProcessToken(SafeHandle process, int access)
        {
            SafeAccessTokenHandle token;
            if (!NativeOpenProcessToken(process, access, out token))
                throw new Win32Exception();

            return token;
        }

        [DllImport("Advapi32.dll", EntryPoint = "RevertToSelf", SetLastError = true)]
        private static extern bool NativeRevertToSelf();

        public static void RevertToSelf()
        {
            if (!NativeRevertToSelf())
                throw new Win32Exception();
        }

        [DllImport("User32.dll", EntryPoint = "SetProcessWindowStation", SetLastError = true)]
        private static extern bool NativeSetProcessWindowStation(
            SafeHandle hWinSta);

        public static void SetProcessWindowStation(SafeHandle station)
        {
            if (!NativeSetProcessWindowStation(station))
                throw new Win32Exception();
        }

        [DllImport("Advapi32.dll", EntryPoint = "SetTokenInformation", SetLastError = true)]
        private static extern bool NativeSetTokenInformation(
            SafeHandle TokenHandle,
            int TokenInformationClass,
            SafeHandle TokenInformation,
            int TokenInformationLength);

        public static void SetTokenInformation(SafeHandle token, int infoClass, SafeHandle info, int length)
        {
            if (!NativeSetTokenInformation(token, infoClass, info, length))
                throw new Win32Exception();
        }

        [DllImport("Userenv.dll", EntryPoint = "UnloadUserProfile", SetLastError = true)]
        private static extern bool NativeUnloadUserProfile(
            SafeHandle token,
            IntPtr hProfile);

        public static void UnloadUserProfile(SafeHandle token, IntPtr profile)
        {
            if (!NativeUnloadUserProfile(token, profile))
                throw new Win32Exception();
        }
    }

    [Flags]
    public enum CreateDesktopFlags : uint
    {
        NONE = 0x0000,
        DF_ALLOWOTHERACCOUNTHOOK = 0x0001,
    }

    [Flags]
    public enum CreateStationFlags : uint
    {
        NONE = 0x0000,
        CWF_CREATE_ONLY = 0x0001,
    }

    [Flags]
    public enum PrivilegeAttributes : uint
    {
        Disabled = 0x00000000,
        EnabledByDefault = 0x00000001,
        Enabled = 0x00000002,
        Removed = 0x00000004,
        UsedForAccess = 0x80000000,
    }

    public enum SecurityImpersonationLevel : uint
    {
        SecurityAnonymous = 0,
        SecurityIdentification = 1,
        SecurityImpersonation = 2,
        SecurityDelegation = 3,
    }

    public enum TokenType : uint
    {
        Primary = 1,
        Impersonation = 2,
    }

    public class SafeDesktopHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeDesktopHandle() : base(true) { }
        public SafeDesktopHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            return Native.CloseDesktop(handle);
        }
    }

    public class SafeMemoryBuffer : SafeHandleZeroOrMinusOneIsInvalid
    {
        public int Length { get; internal set; }

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

    public class SafeStationHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeStationHandle() : base(true) { }
        public SafeStationHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            return Native.CloseWindowStation(handle);
        }
    }
}
'@

Function New-ProcessExSession {
    [CmdletBinding()]
    param (
        [String]
        $FilePath = (Get-Process -Id $pid).Path,

        [Parameter()]
        [String]
        $WorkingDirectory,

        [Parameter()]
        [System.Collections.IDictionary]
        $Environment,

        [Parameter()]
        [ProcessEx.StartupInfo]
        $StartupInfo = (New-StartupInfo),

        [Parameter()]
        [Switch]
        $UseNewEnvironment,

        [Parameter()]
        [Switch]
        $DisableInheritance,

        [Parameter()]
        [System.Runtime.InteropServices.SafeHandle]
        $Token
    )

    $StartupInfo.ShowWindow = [ProcessEx.WindowStyle]::Hide
    $params = @{
        FilePath = $FilePath
        StartupInfo = $StartupInfo
        UseNewEnvironment = $UseNewEnvironment
        DisableInheritance = $DisableInheritance
        PassThru = $true
    }
    if ($WorkingDirectory) {
        $params.WorkingDirectory = $WorkingDirectory
    }
    if ($Environment.Count -gt 0) {
        $params.Environment = $Environment
    }
    if ($Token) {
        $params.Token = $Token
    }

    $proc = Start-ProcessEx @params
    try {
        $connInfo = [System.Management.Automation.Runspaces.NamedPipeConnectionInfo]::new($proc.ProcessId)
        $rs = [RunspaceFactory]::CreateRunspace($connInfo)
        $rs.Open()

        $ps = [PowerShell]::Create()
        $ps.Runspace = $rs
        $location = if ($WorkingDirectory) { $WorkingDirectory } else { $pwd.Path }
        [void]$ps.AddCommand("Set-Location").AddParameter("Path", $location).AddStatement()

        $testModulesPath = [IO.Path]::Combine($PSScriptRoot, '..', 'output', 'Modules')
        Get-ChildItem -LiteralPath $testModulesPath -Exclude ProcessEx | ForEach-Object -Process {
            [void]$ps.AddCommand("Import-Module").AddParameter("Name", $_.FullName).AddStatement()
        }

        [void]$ps.AddScript($PSCommandPath)
        [void]$ps.Invoke()

        # Unforunately this isn't publicly exposed so use reflection. It hasn't changed anytime soon and these are
        # tests so the risks of using the internal cstr is minimal.
        $cstr = [System.Management.Automation.Runspaces.PSSession].GetConstructor(
            'NonPublic, Instance', $null, [type[]]$rs.GetType(), $null)
        $session = $cstr.Invoke(@($rs))
        Add-Member -InputObject $session -NotePropertyName Process -NotePropertyValue $Proc

        $session
    }
    catch {
        $proc | Stop-Process -Force -ErrorAction SilentlyContinue
        $PSCmdlet.WriteError($_)
    }
}

Function New-ProcessWithSession {
    [CmdletBinding(DefaultParameterSetName = "Credential")]
    param (
        [String]
        $FilePath = (Get-Process -Id $pid).Path,

        [Parameter()]
        [String]
        $WorkingDirectory,

        [Parameter()]
        [System.Collections.IDictionary]
        $Environment,

        [Parameter()]
        [ProcessEx.StartupInfo]
        $StartupInfo = (New-StartupInfo),

        [Parameter(Mandatory, ParameterSetName = "Token")]
        [System.Runtime.InteropServices.SafeHandle]
        $Token,

        [Parameter(Mandatory, ParameterSetName = "Credential")]
        [PSCredential]
        $Credential,

        [Switch]
        $NetCredentialsOnly,

        [Switch]
        $WithProfile
    )

    $StartupInfo.ShowWindow = [ProcessEx.WindowStyle]::Hide
    $params = @{
        FilePath = $FilePath
        StartupInfo = $StartupInfo
        NetCredentialsOnly = $NetCredentialsOnly
        WithProfile = $WithProfile
        PassThru = $true
    }
    if ($WorkingDirectory) {
        $params.WorkingDirectory = $WorkingDirectory
    }
    if ($Environment.Count -gt 0) {
        $params.Environment = $Environment
    }
    if ($Token) {
        $params.Token = $Token
    }
    if ($Credential) {
        $params.Credential = $Credential
    }

    $proc = Start-ProcessWith @params
    try {
        $connInfo = [System.Management.Automation.Runspaces.NamedPipeConnectionInfo]::new($proc.ProcessId)
        $rs = [RunspaceFactory]::CreateRunspace($connInfo)
        $rs.Open()

        $ps = [PowerShell]::Create()
        $ps.Runspace = $rs
        $location = if ($WorkingDirectory) { $WorkingDirectory } else { $pwd.Path }
        [void]$ps.AddCommand("Set-Location").AddParameter("Path", $location).AddStatement()

        $testModulesPath = [IO.Path]::Combine($PSScriptRoot, '..', 'output', 'Modules')
        Get-ChildItem -LiteralPath $testModulesPath -Exclude ProcessEx | ForEach-Object -Process {
            [void]$ps.AddCommand("Import-Module").AddParameter("Name", $_.FullName).AddStatement()
        }

        [void]$ps.AddScript($PSCommandPath)
        [void]$ps.Invoke()

        # Unforunately this isn't publicly exposed so use reflection. It hasn't changed anytime soon and these are
        # tests so the risks of using the internal cstr is minimal.
        $cstr = [System.Management.Automation.Runspaces.PSSession].GetConstructor(
            'NonPublic, Instance', $null, [type[]]$rs.GetType(), $null)
        $session = $cstr.Invoke(@($rs))
        Add-Member -InputObject $session -NotePropertyName Process -NotePropertyValue $Proc

        $session
    }
    catch {
        $proc | Stop-Process -Force -ErrorAction SilentlyContinue
        $PSCmdlet.WriteError($_)
    }
}

Function Remove-ProcessExSession {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline)]
        [System.Management.Automation.Runspaces.PSSession[]]
        $Session
    )

    process {
        foreach ($s in $Session) {
            $s | Remove-PSSession
            if ($s.PSObject.Properties.Name.Contains('Process')) {
                $s.Process | Stop-Process -Force
                $s.Process.Process.Dispose()
                $s.Process.Thread.Dispose()
            }
        }
    }
}

Function Get-TokenPrivilege {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [SafeHandle]
        $Token
    )

    process {
        $buffer = [ProcessExTests.Native]::GetTokenInformation($Token,
            3 # TokenPrivileges
        )
        try {
            $tokenPrivileges = [Marshal]::PtrToStructure($buffer.DangerousGetHandle(),
                ([type][ProcessExTests.Helpers+TOKEN_PRIVILEGES]))

            $ptrOffset = [IntPtr]::Add($buffer.DangerousGetHandle(), 4)
            for ($i = 0; $i -lt $tokenPrivileges.Privilegecount; $i++) {
                $luidAndAttributes = [Marshal]::PtrToStructure($ptrOffset, ([type][ProcessExTests.Helpers+LUID_AND_ATTRIBUTES]))
                $ptrOffset = [IntPtr]::Add($ptrOffset, [Marshal]::SizeOf($luidAndAttributes))

                [PSCustomObject]@{
                    Token = $Token
                    Name = [ProcessExTests.Native]::LookupPrivilegeName($null, $luidAndAttributes.Luid)
                    State = $luidAndAttributes.Attributes
                }
            }
        }
        finally {
            $buffer.Dispose()
        }
    }
}

Function Get-TokenElevationType {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [System.Runtime.InteropServices.SafeHandle]
        $Token
    )

    process {
        $buffer = [ProcessExTests.Native]::GetTokenInformation($Token,
            18 # TokenElevationType
        )
        try {
            [System.Runtime.InteropServices.Marshal]::ReadInt32($buffer.DangerousGetHandle())
        }
        finally {
            $buffer.Dispose()
        }
    }
}

Function Get-TokenLinkedToken {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [System.Runtime.InteropServices.SafeHandle]
        $Token
    )

    process {
        $buffer = [ProcessExTests.Native]::GetTokenInformation($Token,
            19 # TokenLinkedToken
        )
        try {
            [Microsoft.Win32.SafeHandles.SafeAccessTokenHandle]::new(
                [System.Runtime.InteropServices.Marshal]::ReadIntPtr($buffer.DangerousGetHandle()))
        }
        finally {
            $buffer.Dispose()
        }
    }
}

Function Get-TokenSessionId {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [System.Runtime.InteropServices.SafeHandle]
        $Token
    )

    process {
        $buffer = [ProcessExTests.Native]::GetTokenInformation($Token,
            12 # TokenSessionId
        )
        try {
            [System.Runtime.InteropServices.Marshal]::ReadInt32($buffer.DangerousGetHandle())
        }
        finally {
            $buffer.Dispose()
        }
    }
}

Function Get-ProcessToken {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline)]
        $Process,

        [Parameter()]
        [System.Security.Principal.TokenAccessLevels]
        $Access = ([System.Security.Principal.TokenAccessLevels]::Query)
    )

    process {
        try {
            $proc = Get-ProcessEx -Process $Process -Access QueryInformation
            [ProcessExTests.Native]::OpenProcessToken($proc.Process, $Access)
        }
        catch {
            $PSCmdlet.WriteError($_)
        }
    }
}

Function Get-ElevatedToken {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [SafeHandle]
        $Token
    )

    process {
        $privInfo = Get-ProcessPrivilege -Name SeTcbPrivilege
        if (-not $privInfo.IsRemoved) {
            if (-not $privInfo.Enabled) {
                Enable-ProcessPrivilege -Name SeTcbPrivilege
            }

            Get-TokenLinkedToken -Token $token
            return
        }

        # Enumerate all the processes and try and impersonate ones with the SeTcbPrivilege. With this privilege the
        # code can get the linked token from the tokens supplied by the caller. Without the SeTcbPrivilege, the linked
        # token will only have identification rights which cannot be used to start a new process. With this privilege
        # it will have impersonation rights which can be used to start a new process.
        $elevatedToken = Get-Process |
            Get-ProcessToken -Access Duplicate, Impersonate, Query -ErrorAction SilentlyContinue |
            Get-TokenPrivilege |
            Where-Object Name -EQ "SeTcbPrivilege" |
            ForEach-Object -Process {
                [ProcessExTests.Native]::ImpersonateLoggedOnUser($_.Token)
                try {
                    Get-TokenLinkedToken -Token $token
                }
                finally {
                    [ProcessExTests.Native]::RevertToSelf()
                }
            } |
            Select-Object -First 1

        if (-not $elevatedToken) {
            throw "Could not get handle on SYSTEM token for elevating context"
        }

        $elevatedToken
    }
}

Function Set-TokenSessionId {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [System.Runtime.InteropServices.SafeHandle]
        $Token,

        [Parameter(Mandatory)]
        [int]
        $SessionId
    )

    process {
        $buffer = [ProcessExTests.SafeMemoryBuffer]::new(4)
        try {
            [System.Runtime.InteropServices.Marshal]::WriteInt32($buffer.DangerousGetHandle(), $SessionId)
            [ProcessExTests.Native]::SetTokenInformation($Token,
                12, # TokenSessionId
                $buffer,
                $buffer.Length)
        }
        finally {
            $buffer.Dispose()
        }
    }
}

Function New-LocalAccount {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [String]
        $Name,

        [Parameter(Mandatory)]
        [SecureString]
        $Password,

        [String]
        $Description,

        [Switch]
        $PasswordNeverExpires,

        [Switch]
        $UserMayNotChangePassword,

        [String[]]
        $GroupMembership
    )

    # New-LocalUser doesn't work on 32-bit powershell which is needed for the tests

    $ADS_UF_PASSWD_CANT_CHANGE = 64
    $ADS_UF_DONT_EXPIRE_PASSWD = 65536
    $adsi = [ADSI]"WinNT://$env:COMPUTERNAME"

    $userAdsi = $adsi.Create('User', $Name)
    [void]$userAdsi.SetPassword([pscredential]::new("dummy", $Password).GetNetworkCredential().Password)
    [void]$userAdsi.SetInfo()

    if ($Description) {
        $userAdsi.Description = $Description
    }

    if ($PasswordNeverExpires) {
        $userAdsi.UserFlags.Value = $userAdsi.UserFlags.Value -bor $ADS_UF_DONT_EXPIRE_PASSWD
    }
    else {
        $userAdsi.UserFlags.Value = $userAdsi.UserFlags.Value -band -bnot $ADS_UF_DONT_EXPIRE_PASSWD
    }

    if ($UserMayNotChangePassword) {
        $userAdsi.UserFlags.Value = $userAdsi.UserFlags.Value -bor $ADS_UF_PASSWD_CANT_CHANGE
    }
    else {
        $userAdsi.UserFlags.Value = $userAdsi.UserFlags.Value -band -bnot $ADS_UF_PASSWD_CANT_CHANGE
    }

    [void]$userAdsi.SetInfo()

    if ($GroupMembership) {
        [string[]]$sidsToAdd = @($GroupMembership | ForEach-Object {
                [System.Security.Principal.NTAccount]::new($_).Translate([System.Security.Principal.SecurityIdentifier]).Value
            })
        [string[]]$existingSids = @($userAdsi.Groups() | ForEach-Object -Process {
                $rawSid = $_.GetType().InvokeMember("ObjectSid", "GetProperty", $null, $_, $null)
                [System.Security.Principal.SecurityIdentifier]::new($rawSid, 0).Value
            })
        $toAdd = [Linq.Enumerable]::Except($sidsToAdd, $existingSids)

        foreach ($group in $toAdd) {
            $groupSid = [System.Security.Principal.SecurityIdentifier]::new($group)
            $groupAdsi = $adsi.Children | Where-Object {
                if ($_.SchemaClassName -ne "Group") {
                    return $false
                }

                $sid = [System.Security.Principal.SecurityIdentifier]::new($_.ObjectSid.Value, 0).Value
                return $sid -eq $groupSid
            } | Select-Object -First 1

            [void]$groupAdsi.Add($userAdsi.Path)
        }
    }

    [System.Security.Principal.SecurityIdentifier]::new($userAdsi.ObjectSid.Value, 0)
}

Function Remove-LocalAccount {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [System.Security.Principal.IdentityReference]
        $Account
    )

    # New-LocalUser doesn't work on 32-bit powershell which is needed for the tests

    $sidToRemove = $Account.Translate([System.Security.Principal.SecurityIdentifier])
    $adsi = [ADSI]"WinNT://$env:COMPUTERNAME"

    $adsi.Children | Where-Object {
        if ($_.SchemaClassName -ne 'User') { return $false }

        $sid = [System.Security.Principal.SecurityIdentifier]::new($_.ObjectSid.Value, 0)
        $sid -eq $sidToRemove
    } | ForEach-Object {
        $adsi.Delete("User", $_.Name.Value)
    }
}

Function Global:Complete {
    [OutputType([System.Management.Automation.CompletionResult])]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]
        $Expression
    )

    [System.Management.Automation.CommandCompletion]::CompleteInput(
        $Expression,
        $Expression.Length,
        $null).CompletionMatches
}
