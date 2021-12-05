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
privileges not usually granted to an admin account. These are the privileges
enabled for the tests to run:

    SeTcbPrivilege - Needed to easily get the linked token when testing -Token
    SeAssignPrimaryTokenPrivilege - Needed for Start-ProcessEx with -Token (CreateProcessAsUser)

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

. ([IO.Path]::Combine((Split-Path $PSScriptRoot -Parent), "tests", "common.ps1"))

$desiredPrivileges = 'SeAssignPrimaryTokenPrivilege', 'SeTcbPrivilege'
$password = "Password123!"
$userParams = @{
    Name = "ProcessEx-User"
    Password = (ConvertTo-SecureString -AsPlainText -Force -String $password)
    Description = "Test user for ProcessEx with higher privileges"
    PasswordNeverExpires = $true
    UserMayNotChangePassword = $true
    GroupMembership = "Administrators"
}
$user = New-LocalAccount @userParams

$token = $stdoutPipe = $stderrPipe = $null
try {
    Add-WindowsRight -Name $desiredPrivileges -Account $user

    $token = [ProcessExTests.Native]::LogonUser(
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
        WindowStyle = "Hide"
    }
    $si = New-StartupInfo @siParams

    $pi = [ProcessExTests.Native]::LoadUserProfile($token, $userParams.Name)
    try {
        $profileDir = [ProcessExTests.Native]::GetUserProfileDirectory($token)
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
        [ProcessExTests.Native]::UnloadUserProfile($token, $pi.hProfile)
        [ProcessExTests.Native]::DeleteProfile($user.Value, [NullString]::Value, [NullString]::Value)

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
    Remove-WindowsRight -Name $desiredPrivileges -Account $user
    Remove-LocalAccount -Account $user
}
