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
Start-ProcessWith [-FilePath] <String> [-ArgumentList <String[]>] -Credential <PSCredential>
 [-WorkingDirectory <String>] [-StartupInfo <StartupInfo>] [-CreationFlags <CreationFlags>]
 [-Environment <IDictionary>] [-WithProfile] [-NetCredentialsOnly] [-Wait] [-PassThru] [<CommonParameters>]
```

### FilePathToken
```
Start-ProcessWith [-FilePath] <String> [-ArgumentList <String[]>] -Token <SafeHandle>
 [-WorkingDirectory <String>] [-StartupInfo <StartupInfo>] [-CreationFlags <CreationFlags>]
 [-Environment <IDictionary>] [-WithProfile] [-NetCredentialsOnly] [-Wait] [-PassThru] [<CommonParameters>]
```

### CommandLineCredential
```
Start-ProcessWith -CommandLine <String> [-ApplicationName <String>] -Credential <PSCredential>
 [-WorkingDirectory <String>] [-StartupInfo <StartupInfo>] [-CreationFlags <CreationFlags>]
 [-Environment <IDictionary>] [-WithProfile] [-NetCredentialsOnly] [-Wait] [-PassThru] [<CommonParameters>]
```

### CommandLineToken
```
Start-ProcessWith -CommandLine <String> [-ApplicationName <String>] -Token <SafeHandle>
 [-WorkingDirectory <String>] [-StartupInfo <StartupInfo>] [-CreationFlags <CreationFlags>]
 [-Environment <IDictionary>] [-WithProfile] [-NetCredentialsOnly] [-Wait] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Like `Start-Process` but exposes a few more low level options and focuses specifically on the Win32 API `CreateProcessWithLogon` or `CreateProcessWithToken`.
Use this instead of `Start-ProcessEx` if you wish to start a process as another user but do no have the `SeAssignPrimaryTokenPrivilege` privileges required by the `-Token` parameter on that cmdlet.

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

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
Defaults to `CreateNewConsole`, `CreateDefaultErrorMode`, `CreateNewProcessGroup` to ensure the console application is created in a new console window instead of sharing the existing console.
You cannot set `CreateSuspended` with the `-Wait` parameter.
It is not possible to omit `CreateNewConsole`, a process spawned by this cmdlet must be done in a new console.

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
This cannot be set with `-CreationFlags CreateSuspended`.

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
The working directory to set for the new process, defaults to the current process working dir if not defined.

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
