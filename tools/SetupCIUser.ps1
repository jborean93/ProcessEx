<#
.SYNOPSIS
Set up highly privileged account for CI testing.

.PARAMETER Credential
The credential to use when creating the test account for CI.
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [PSCredential]
    $Credential
)

$userParams = @{
    Name = $Credential.UserName
    Password = $Credential.Password
    Description = "Test user for CI with higher privileges"
    AccountNeverExpires = $true
    PasswordNeverExpires = $true
    UserMayNotChangePassword = $true
}
$user = New-LocalUser @userParams
Add-LocalGroupMember -Group Administrators -Member $user
Add-WindowsRight -Name SeAssignPrimaryTokenPrivilege -Account @(
    $user.SID,
    ([System.Security.Principal.WindowsIdentity]::GetCurrent().User)
)
