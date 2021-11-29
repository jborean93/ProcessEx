#Requires -Module ProcessEx
#Requires -Module PSPrivilege

using namespace Microsoft.Win32.SafeHandles
using namespace System.Runtime.InteropServices
using namespace System.Security.AccessControl
using namespace System.Security.Principal

<#
.SYNOPSIS
Runs the process as a user with sensitive privileges.

.PARAMETER Executable
The executable to run as the elevated user.

.PARAMETER ArgumentList
The arguments to run with the executable.

.NOTES
This must be running as an administrator already. it doesn't elevated a limited
account to an admin but rather creates a temporary admin account with
privileges not usually granted to an admin account. These privileges are
required to for tests to run.

Due to inheritance problems the new process cannot inherit the same console so
a simple redirection will take place. This means the colour is not redirected
to the parent. Unfortunately to inherit the same console CreateProcessAsUser
needs to be called but that requires a sensitive privilege not normally granted
to administrators.
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [String]
    $Executable,

    [Parameter(Mandatory)]
    [String[]]
    $ArgumentList
)

$ErrorActionPreference = 'Stop'

# TODO: Use PSAccessToken for the majority of this
Add-Type -TypeDefinition @'
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Native
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

    public class Methods
    {
        [DllImport("Userenv.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateEnvironmentBlock", SetLastError = true)]
        private static extern bool NativeCreateEnvironmentBlock(
            out SafeEnvironmentBlock lpEnvironment,
            SafeHandle hToken,
            bool bInherit);

        public static SafeEnvironmentBlock CreateEnvironmentBlock(SafeHandle token, bool inherit)
        {
            if (!NativeCreateEnvironmentBlock(out var block, token, inherit))
                throw new Win32Exception();

            return block;
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

        [DllImport("Userenv.dll", SetLastError = true)]
        public static extern bool DestroyEnvironmentBlock(
            IntPtr lpEnvironment);

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

        [DllImport("Kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true)]
        private static extern IntPtr NativeGetStdHandle(int nStdHandle);

        public static SafeFileHandle GetStdHandle(int handleId)
        {
            IntPtr handle = NativeGetStdHandle(handleId);
            if (handle == (IntPtr)(-1))
                throw new Win32Exception();

            return new SafeFileHandle(handle, false);
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

        [DllImport("Advapi32.dll", EntryPoint = "ImpersonateLoggedOnUser", SetLastError = true)]
        private static extern bool NativeImpersonateLoggedOnUser(
            SafeHandle hToken);

        public static void ImpersonateLoggedOnUser(SafeHandle token)
        {
            if (!NativeImpersonateLoggedOnUser(token))
                throw new Win32Exception();
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

        public static SafeAccessTokenHandle LogonUser(string username, string domain, string password, int domainType,
            int logonProvider)
        {
            SafeAccessTokenHandle token;
            if (!LogonUserW(username, domain, password, domainType, logonProvider, out token))
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
    public enum PrivilegeAttributes : uint
    {
        Disabled = 0x00000000,
        EnabledByDefault = 0x00000001,
        Enabled = 0x00000002,
        Removed = 0x00000004,
        UsedForAccess = 0x80000000,
    }

    public class SafeEnvironmentBlock : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeEnvironmentBlock() : base(true) { }

        protected override bool ReleaseHandle()
        {
            return Methods.DestroyEnvironmentBlock(handle);
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
}
'@

Function Get-TokenPrivilege {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [SafeHandle]
        $Token
    )

    process {
        $buffer = [Native.Methods]::GetTokenInformation($Token,
            3 # TokenPrivileges
        )
        try {
            $tokenPrivileges = [Marshal]::PtrToStructure($buffer.DangerousGetHandle(),
                ([type][Native.Helpers+TOKEN_PRIVILEGES]))

            $ptrOffset = [IntPtr]::Add($buffer.DangerousGetHandle(), 4)
            for ($i = 0; $i -lt $tokenPrivileges.Privilegecount; $i++) {
                $luidAndAttributes = [Marshal]::PtrToStructure($ptrOffset, ([type][Native.Helpers+LUID_AND_ATTRIBUTES]))
                $ptrOffset = [IntPtr]::Add($ptrOffset, [Marshal]::SizeOf($luidAndAttributes))

                [PSCustomObject]@{
                    Token = $Token
                    Name = [Native.Methods]::LookupPrivilegeName($null, $luidAndAttributes.Luid)
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
        [SafeHandle]
        $Token
    )

    process {
        $buffer = [Native.Methods]::GetTokenInformation($Token,
            18 # TokenElevationType
        )
        try {
            [Marshal]::ReadInt32($buffer.DangerousGetHandle())
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
        [SafeHandle]
        $Token
    )

    process {
        $buffer = [Native.Methods]::GetTokenInformation($Token,
            19 # TokenLinkedToken
        )
        try {
            [SafeAccessTokenHandle]::new([Marshal]::ReadIntPtr($buffer.DangerousGetHandle()))
        }
        finally {
            $buffer.Dispose()
        }
    }
}

Function Get-TokenEnvironment {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [SafeHandle]
        $Token
    )

    process {
        $envBlock = [Native.Methods]::CreateEnvironmentBlock($token, $false)
        try {
            $environment = [Ordered]@{}

            $ptr = $envBlock.DangerousGetHandle()

            # The env block are UTF-16 codepoints that are null terminated ending with an empty string.
            $currentValue = [System.Text.StringBuilder]::new()
            while ($true) {
                # Needs an unsigned value so just hack it into an int32 which will never be negative
                $char = [char][int32]('0x{0:X4}' -f ([Marshal]::ReadInt16($ptr)))
                $ptr = [IntPtr]::Add($ptr, 2)

                if ($char -eq 0) {
                    if ($currentValue.Length -eq 0) {
                        break
                    }
                    else {
                        $k, $v = $currentValue.ToString().Split('=', 2)
                        $environment[$k] = $v
                        $currentValue = [System.Text.StringBuilder]::new()
                    }
                }
                else {
                    [void]$currentValue.Append($char)
                }
            }

            $environment
        }
        finally {
            $envBlock.Dispose()
        }
    }
}

Function Get-ProcessToken {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline)]
        [System.Diagnostics.Process]
        $Process,

        [Parameter()]
        [TokenAccessLevels]
        $Access = ([TokenAccessLevels]::Query)
    )

    process {
        try {
            [Native.Methods]::OpenProcessToken($_.SafeHandle, $Access)
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
        # Enumerate all the processes and try and impersonate ones with the SeTcbPrivilege. With this privilege the
        # code can get the linked token from the tokens supplied by the caller. Without the SeTcbPrivilege, the linked
        # token will only have identification rights which cannot be used to start a new process. With this privilege
        # it will have impersonation rights which can be used to start a new process.
        $elevatedToken = Get-Process -IncludeUserName |
            Get-ProcessToken -Access Duplicate, Impersonate, Query -ErrorAction SilentlyContinue |
            Get-TokenPrivilege |
            Where-Object Name -eq "SeTcbPrivilege" |
            ForEach-Object -Process {
                [Native.Methods]::ImpersonateLoggedOnUser($_.Token)
                try {
                    Get-TokenLinkedToken -Token $token
                }
                finally {
                    [Native.Methods]::RevertToSelf()
                }
            } |
            Select-Object -First 1

        if (-not $elevatedToken) {
            throw "Could not get handle on SYSTEM token for elevating context"
        }

        $elevatedToken
    }
}

$desiredPrivileges = 'SeAssignPrimaryTokenPrivilege'
$password = "Password123!"
$userParams = @{
    Name = "ProcessEx-User"
    Password = (ConvertTo-SecureString -AsPlainText -Force -String $password)
    Description = "Test user for ProcessEx with higher privileges"
    AccountNeverExpires = $true
    PasswordNeverExpires = $true
    UserMayNotChangePassword = $true
}
$user = New-LocalUser @userParams

$token = $stdoutPipe = $stderrPipe = $null
try {
    Add-LocalGroupMember -Group Administrators -Member $user
    Add-WindowsRight -Name $desiredPrivileges -Account $user.SID

    $token = [Native.Methods]::LogonUser(
        $userParams.Name,
        $env:COMPUTERNAME,
        $password,
        2, # LOGON32_LOGON_INTERACTIVE
        0 # LOGON32_PROVIDER_DEFAULT
    )

    # When running with UAC the token returned here will be the limited token (stripped out groups and privileges).
    # This will retrieve the full token to be used with Start-ProcessWith
    $elevationType = Get-TokenElevationType -Token $token
    if ($elevationType -eq 3) { # TokenElevationTypeLimited
        $elevatedToken = Get-ElevatedToken -Token $token
        $token.Dispose()
        $token = $elevatedToken
    }

    $stdout = [Console]::OpenStandardOutput()
    $stdoutPipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None")
    $stdoutTask = $stdoutPipe.CopyToAsync($stdout)

    $stderr = [Console]::OpenStandardError()
    $stderrPipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "None")
    $stderrTask = $stderrPipe.CopyToAsync($stderr)

    $siParams = @{
        StandardOutput = $stdoutPipe.ClientSafePipeHandle
        StandardError = $stderrPipe.ClientSafePipeHandle
    }
    $si = New-StartupInfo @siParams

    $pi = [Native.Methods]::LoadUserProfile($token, $userParams.Name)
    try {
        $profileDir = [Native.Methods]::GetUserProfileDirectory($token)
        $environment = Get-TokenEnvironment -Token $token
        $environment.PATH += "$([IO.Path]::PathSeparator)$([IO.Path]::Combine($profileDir, ".dotnet", "tools"))"

        $procParams = @{
            FilePath = $Executable
            ArgumentList = $ArgumentList
            WorkingDirectory = (Get-Location -PSProvider FileSystem).Path
            Environment = $environment
            Token = $token
            StartupInfo = $si
            Wait = $true
            PassThru = $true
        }
        $res = Start-ProcessWith @procParams
    }
    finally {
        [Native.Methods]::UnloadUserProfile($token, $pi.hProfile)
        [Native.Methods]::DeleteProfile($user.SID.Value, [NullString]::Value, [NullString]::Value)

        # DeleteProfile does not always delete everything, check manually
        if (Test-Path -Path $profileDir) {
            Remove-Item -Path $profileDir -Force -Recurse
        }
    }

    # Wait until the output pipes have been flushed
    $stdoutPipe.DisposeLocalCopyOfClientHandle()
    $stderrPipe.DisposeLocalCopyOfClientHandle()
    while (-not $stdoutTask.AsyncWaitHandle.WaitOne(100)) { }
    while (-not $stderrTask.AsyncWaitHandle.WaitOne(100)) { }

    $Host.SetShouldExit($res.ExitCode)
}
finally {
    if ($stdoutPipe) { $stdoutPipe.Dispose() }
    if ($stderrPipe) { $stderrPipe.Dispose() }
    if ($token) { $token.Dispose() }
    Remove-WindowsRight -Name $desiredPrivileges -Account $user.SID
    $user | Remove-LocalUser
}
