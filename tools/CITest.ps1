<#
.SYNOPSIS
Runs the process as a user with sensitive privileges.

.PARAMETER Executable
The executable to run as the elevated user.

.PARAMETER Argument
The arguments to run with the executable.
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [String]
    $Executable,

    [Parameter(Mandatory)]
    [String]
    $Argument
)

$token = $null
$password = "Password123!"
$userParams = @{
    Name = "CITestUser"
    Password = (ConvertTo-SecureString -AsPlainText -Force -String $password)
    Description = "Test user for CI with higher privileges"
    AccountNeverExpires = $true
    PasswordNeverExpires = $true
    UserMayNotChangePassword = $true
}
$user = New-LocalUser @userParams

try {
    Add-LocalGroupMember -Group Administrators -Member $user
    Add-WindowsRight -Name SeAssignPrimaryTokenPrivilege -Account $user.SID

    Import-Module -Name ./output/ProcessEx

    Add-Type -Namespace Win32 -Name Native -MemberDefinition @'
[DllImport("Kernel32.dll")]
public static extern IntPtr GetStdHandle(int nStdHandle);

[DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
public static extern bool LogonUserW(
    string lpszUsername,
    string lpszDomain,
    string lpszPassword,
    int dwLogonType,
    int dwLogonProvider,
    out Microsoft.Win32.SafeHandles.SafeAccessTokenHandle phToken);
'@

    $res = [Win32.Native]::LogonUserW(
        $userParams.Name,
        $env:COMPUTERNAME,
        $password,
        2, # LOGON32_LOGON_INTERACTIVE
        0, # LOGON32_PROVIDER_DEFAULT
        [ref]$token
    ); $err = [System.Runtime.InteropServices.Marshal]::GetLastWin32Error()
    if (-not $res) {
        $exp = [System.ComponentModel.Win32Exception]::new($err)
        throw "Failed to log on elevated user: $($exp.Message)"
    }

    $stdout = [Microsoft.Win32.SafeHandles.SafeWaitHandle]::new([Win32.Native]::GetStdHandle(-11), $false)
    $stderr = [Microsoft.Win32.SafeHandles.SafeWaitHandle]::new([Win32.Native]::GetStdHandle(-12), $false)

    $si = New-StartupInfo -StandardOutput $stdout -StandardError $stderr
    $res = Start-ProcessWith whoami.exe /all -Token $token -StartupInfo $si -Wait -PassThru

    exit 0 # TODO: Add ExitCode to ProcessInfo
}
finally {
    if ($token) { $token.Dispose() }
    $user | Remove-LocalUser
}
