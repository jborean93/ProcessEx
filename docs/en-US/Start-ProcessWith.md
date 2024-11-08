---
external help file: ProcessEx.dll-Help.xml
Module Name: ProcessEx
online version:
schema: 2.0.0
---

# Start-ProcessWith

## SYNOPSIS
Start a new process with credentials or a token.

## SYNTAX

### FilePathCredential (Default)
```
Start-ProcessWith [-FilePath] <String> [-ArgumentList <String[]>] [-ArgumentEscaping <ArgumentEscapingMode>]
 -Credential <PSCredential> [-WorkingDirectory <String>] [-StartupInfo <StartupInfo>]
 [-CreationFlags <CreationFlags>] [-Environment <IDictionary>] [-WithProfile] [-NetCredentialsOnly] [-Wait]
 [-PassThru] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### FilePathToken
```
Start-ProcessWith [-FilePath] <String> [-ArgumentList <String[]>] [-ArgumentEscaping <ArgumentEscapingMode>]
 -Token <SafeHandle> [-WorkingDirectory <String>] [-StartupInfo <StartupInfo>] [-CreationFlags <CreationFlags>]
 [-Environment <IDictionary>] [-WithProfile] [-NetCredentialsOnly] [-Wait] [-PassThru]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### CommandLineCredential
```
Start-ProcessWith -CommandLine <String> [-ApplicationName <String>] -Credential <PSCredential>
 [-WorkingDirectory <String>] [-StartupInfo <StartupInfo>] [-CreationFlags <CreationFlags>]
 [-Environment <IDictionary>] [-WithProfile] [-NetCredentialsOnly] [-Wait] [-PassThru]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### CommandLineToken
```
Start-ProcessWith -CommandLine <String> [-ApplicationName <String>] -Token <SafeHandle>
 [-WorkingDirectory <String>] [-StartupInfo <StartupInfo>] [-CreationFlags <CreationFlags>]
 [-Environment <IDictionary>] [-WithProfile] [-NetCredentialsOnly] [-Wait] [-PassThru]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Like `Start-Process` but exposes a few more low level options and focuses specifically on the Win32 API `CreateProcessWithLogon` or `CreateProcessWithToken`.
Use this instead of `Start-ProcessEx` if you wish to start a process as another user but do no have the `SeAssignPrimaryTokenPrivilege` privileges required by the `-Token` parameter on that cmdlet.

## EXAMPLES

### Example 1: Start a powershell with explicit credentials
```powershell
PS C:\> $cred = Get-Credential
PS C:\> Start-ProcessWith powershell -Credential $cred
```

Starts PowerShell with the explicit user defined by the credential.
This process will be running with an interactive logon

### Example 2: Start process as another user with arguments
```powershell
PS C:\> $cred = Get-Credential
PS C:\> Start-ProcessWith -FilePath pwsh.exe -ArgumentList '-NoProfile', '-Command', 'echo "hi"' -Credential $cred
```

Starts `pwsh.exe` with command arguments `-NoProfile -Command echo "hi"` as the user specified.

### Example 3: Start process as another user without escaping the command line
```powershell
PS C:\> $cred = Get-Credential
PS C:\> $id = '{e97fa56f-daee-434f-ae00-5fab3d0b054a}'
PS C:\> $cmd = 'msiexec.exe /x {0} /qn TEST="my prop"' -f $id
PS C:\> Start-ProcessWith -CommandLine $cmd -Credential $cred
```

Runs the process with the literal command line value `msiexec.exe /x {e97fa56f-daee-434f-ae00-5fab3d0b054a} /qn TEST="my prop"` as the user specified by the credential.
If the argument `Test="my prop"` was used with `-ArgumentList` it would be escaped as the literal value `"Test=\"my prop\""`.
Using the `-CommandLine` parameter will disable any escaping rules and run the raw command exactly as it was passed in.

### Example 4: Start a process with redirected stdout to a file
```powershell
PS C:\> $cred = Get-Credential
PS C:\> $fs = [IO.File]::Open('C:\stdout.txt', 'Create', 'Write', 'ReadWrite')
PS C:\> try
..   $si = New-StartupInfo -StandardOutput $fs.SafeFileHandle
..   Start-ProcessWith -FilePath cmd.exe /c echo hi -StartupInfo $si -Wait -Credential $cred
.. }
.. finally {
..   $fs.Dispose()
.. }
```

Runs a process as the user specified with the stdout redirected to the file at `C:\stdout.txt`.

### Example 5: Start a process with a token
```powershell
PS C:\> Add-Type -Namespace Advapi32 -Name Methods -MemberDefinition @'
>> [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
>> private static extern bool LogonUserW(
>>     string lpszUsername,
>>     string lpszDomain,
>>     string lpszPassword,
>>     int dwLogonType,
>>     int dwLogonProvider,
>>     out Microsoft.Win32.SafeHandles.SafeAccessTokenHandle phToken);
>>
>> public static Microsoft.Win32.SafeHandles.SafeAccessTokenHandle LogonUser(
>>     string username, string domain, string password, int logonType,
>>     int logonProvider)
>> {
>>     Microsoft.Win32.SafeHandles.SafeAccessTokenHandle token;
>>     if (!LogonUserW(username, domain, password, logonType, logonProvider, out token))
>>         throw new System.ComponentModel.Win32Exception();
>>     return token;
>> }
>> '@
PS C:\> $LOGON32_LOGON_BATCH = 4
PS C:\> $LOGON32_PROVIDER_DEFAULT = 0
PS C:\> $token = [Advapi32.Methods]::LogonUser("user", "domain", "pass",
>>   $LOGON32_LOGON_BATCH, $LOGON32_PROVIDER_DEFAULT)
PS C:\> try {
>>   Start-ProcessWith powershell -Token $token
>> }
>> finally {
>>   $token.Dispose()
>> }
```

Creates a new PowerShell process running under a batch logon of a custom user.
The token is created using PInvoke by calling `LogonUser`.

## PARAMETERS

### -ApplicationName
Used with `-CommandLine` as the full path to the executable to run.
This is useful if the `-CommandLine` executable path contains spaces and you wish to be explicit about what to run.

```yaml
Type: String
Parameter Sets: CommandLineCredential, CommandLineToken
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ArgumentEscaping
The argument escaping mode to use when building the values of `-ArgumentList` to the single command line string.
The default `Standard` will escape the argument list according to the C style rules where whitespace is fully enclosed as a double quoted string.
The `Raw` rule will ignore all escaping and just appeach each argument with a space.
The `Msi` rule will quote the argument `FOO=value with space` as `FOO="value with space"`.

See [ConvertTo-EscapedArgument](./ConvertTo-EscapedArgument.md) for more information.

```yaml
Type: ArgumentEscapingMode
Parameter Sets: FilePathCredential, FilePathToken
Aliases:

Required: False
Position: Named
Default value: Standard
Accept pipeline input: False
Accept wildcard characters: False
```

### -ArgumentList
A list of arguments to run with the `-FilePath`.
These arguments are automatically escaped based on the Win32 C argument escaping rules as done by `ConvertTo-EscapedArgument`.
If you wish to provide arguments as a literal string without escaping use the `-CommandLine` option instead of this and `-FilePath`.

```yaml
Type: String[]
Parameter Sets: FilePathCredential, FilePathToken
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CommandLine
Used instead of `-FilePath` and `-ArgumentList` to run a new process with the literal string provided.
This string is not escaped so you need to ensure it is valid for your use case.
You can optionally specify `-ApplicationName` with this parameter to be more explicit about what executable to run.

```yaml
Type: String
Parameter Sets: CommandLineCredential, CommandLineToken
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CreationFlags
The process CreationFlags to set when starting the process.
Defaults to `NewConsole`, `CreateDefaultErrorMode`, `CreateNewProcessGroup` to ensure the console application is created in a new console window instead of sharing the existing console.
You cannot set `Suspended` with the `-Wait` parameter.
It is not possible to omit `NewConsole`, a process spawned by this cmdlet must be done in a new console.

A suspended process can be started by calling `[ProcessEx.ProcessRunner]::ResumeThread($proc.Thread)`

```yaml
Type: CreationFlags
Parameter Sets: (All)
Aliases:
Accepted values: None, DebugProcess, DebugOnlyThisProcess, Suspended, DetachedProcess, NewConsole, NormalPriorityClass, IdlePriorityClass, HighPriorityClass, RealtimePriorityClass, NewProcessGroup, UnicodeEnvironment, SeparateWowVDM, SharedWowVDM, ForceDOS, BelowNormalPriorityClass, AboveNormalPriorityClass, InheritParentAffinity, InheritCallerPriority, ProtectedProcess, ExtendedStartupinfoPresent, ProcessModeBackgroundBegin, ProcessModeBackgroundEnd, SecureProcess, BreakawayFromJob, PreserveCodeAuthzLevel, DefaultErrorMode, NoWindow, ProfileUser, ProfileKernel, ProfileServer, IgnoreSystemDefault

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Credential
The user credentials to start the new process with.
This will use the `CreateProcessWithLogon` API which logs on the user as an interactive logon or a NewCredential (network access change) logon when `-NetCredentialsOnly` is specified.
This API can be called by non-admin accounts as it has no sensitive privilege requirements.
Because the logon is an interactive logon, UAC restrictions will apply to the new process as it spawns as a limited user token.
Use the `-Token` parameter after getting the linked token (requires admin rights) to bypass this UAC behaviour.

When `-StartupInfo` specifies a custom station/desktop, the function will add the SID specified by the username to the station and desktop's security descriptor with `AllAccess`.
When no startup info or Desktop is not set then the current station/desktop is used without any adjustments to their security descriptors.

```yaml
Type: PSCredential
Parameter Sets: FilePathCredential, CommandLineCredential
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Environment
A dictionary containing explicit environment variables to use for the new process.
These env vars will be used instead of the existing process environment variables if defined.
Use `Get-TokenEnvironment` to generate a new environment block that can be modified as needed for the new process.

```yaml
Type: IDictionary
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FilePath
The executable to start.
Use with `-ArgumentList` to supply a list of argument to this executable.

```yaml
Type: String
Parameter Sets: FilePathCredential, FilePathToken
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NetCredentialsOnly
The credential or token specified will only be used for any outbound authentication attempts, e.g. SMB file access, AD operations, etc.
Any local actions will continue to use the callers access token.
If using `-Credential`, the username must be in the UPN format `username@REALM.COM`.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Return the `ProcessInfo` object for the process that was started.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
New common parameter introduced in PowerShell 7.4.

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StartupInfo
The process StartupInfo details to use when starting the new process.
Use the `New-StartupInfo` command to define this value based on your needs.

```yaml
Type: StartupInfo
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Token
Creates the process to run with the specified access token instead of explicit credentials.
This access token can be retrieved by using other APIs like `LogonUser` or by duplicating an existing access token of a running process.
The token should have been opened with `Query`, `Duplicate`, and `AssignPrimary` rights.
The `AdjustSessionId` and `AdjutDefault` access may also be required if using a token from another session, e.g. a `SYSTEM` token.
This will use the `CreateProcessWithToken` API which requires the `SeImpersonatePrivilege` privileges usually held by administrators on the host.

Be aware that this process will add the Logon Session SID of this token to the station and desktop security descriptor specified by Desktop on `-StartupInfo`.
If no startup info was specified then the current station and desktop is used.
Running this with multiple tokens could hit the maximum allowed size in a ACL if the station/desktop descriptor is not cleaned up.

```yaml
Type: SafeHandle
Parameter Sets: FilePathToken, CommandLineToken
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Wait
Wait for the process and any of the processes they may spawn to finish before returning.
This cannot be set with `-CreationFlags Suspended`.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WithProfile
Loads the user profile specified by the `-Credential` or `-Token`, including the `HKCU` registry hive.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WorkingDirectory
The working directory to set for the new process, defaults to the current filesystem location of the PowerShell process if not defined.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### None
No output if `-PassThru` is not specified.

### ProcessEx.ProcessInfo
If `-PassThru` is specified the cmdlet will output the `ProcessInfo` of the process. This contains the process and starting thread handle as well as the command invocation details.

This object contains the following properties:

- `Executable` - The executable of the process

- `CommandLine` - The command line used to start the process

- `WorkingDirectory` - The working/current directory of the process

- `Process` - The `SafeHandle` of the process created

- `Thread` - The `SafeHandle` of the main thread

- `ProcessId` - Also aliased to `Id`, this is the process identifier

- `ThreadId` - The identifier of the main thread

- `ParentProcessId` - The process identifier of the parent that spawned this process

- `ExitCode` - The exit code of the process, this will not be set if it's still running

- `Environment` - The environment variables of the process

## NOTES
This cmdlet uses the `CreateProcessWithLogon` or `CreateProcessWithToken` APIs which rely on the secondary logon service in Windows.
While it requires fewer privileges than `CreateProcessAsUser` (`Start-ProcessEx -Token ...`) there are some disadvantages:

- The maximum command line length is 1024 characters (`-ArgumentList` or `-CommandLine`)
- You cannot specify the process or thread security attributes for a custom security descriptor or inheribility options on either
- Handles in the current process cannot be inherited into the child process, you can still specify STDIO handles
- You cannot specify any extended startup information like a parent proces, inherited handles, or a pseudo console
- The environment is not inherited from the current process, it will be created from scratch from the new user profile
- The console is always created in a new window and cannot inherit the current console

## RELATED LINKS

[CreateProcessWithLogon](https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createprocesswithlogonw)
[CreateProcessWithToken](https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createprocesswithtokenw)
[Windows Privileges](https://docs.microsoft.com/en-us/windows/win32/secauthz/privilege-constants)
