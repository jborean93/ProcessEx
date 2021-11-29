---
external help file: ProcessEx.dll-Help.xml
Module Name: ProcessEx
online version: github.com/jborean93/ProcessEx/blob/main/docs/en-US/Start-ProcessEx.md
schema: 2.0.0
---

# Start-ProcessEx

## SYNOPSIS
Start a new process.

## SYNTAX

### FilePath (Default)
```
Start-ProcessEx [-FilePath] <String> [-ArgumentList <String[]>] [-WorkingDirectory <String>]
 [-StartupInfo <StartupInfo>] [-CreationFlags <CreationFlags>] [-ProcessAttribute <SecurityAttributes>]
 [-ThreadAttribute <SecurityAttributes>] [-Environment <IDictionary>] [-Token <SafeHandle>]
 [-UseNewEnvironment] [-DisableInheritance] [-Wait] [-PassThru] [<CommonParameters>]
```

### CommandLine
```
Start-ProcessEx -CommandLine <String> [-ApplicationName <String>] [-WorkingDirectory <String>]
 [-StartupInfo <StartupInfo>] [-CreationFlags <CreationFlags>] [-ProcessAttribute <SecurityAttributes>]
 [-ThreadAttribute <SecurityAttributes>] [-Environment <IDictionary>] [-Token <SafeHandle>]
 [-UseNewEnvironment] [-DisableInheritance] [-Wait] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Like `Start-Process` but exposes a few more low level options and focuses specifically on the Win32 API `CreateProcess` or `CreateProcessAsUser` when `-Token` is specified.

## EXAMPLES

### Example 1: Start a new console process
```powershell
PS C:\> Start-ProcessEx -FilePath powershell.exe
```

Starts `powershell.exe` in a new console window.

### Example 2: Start a process with arguments
```powershell
PS C:\> Start-ProcessEx -FilePath pwsh.exe -ArgumentList '-NoProfile', '-Command', 'echo "hi"'
```

Starts `pwsh.exe` with the command arguments `-NoProfile -Command echo "hi"`.

### Example 3: Start a process without escaping the command line
```powershell
PS C:\> $id = '{e97fa56f-daee-434f-ae00-5fab3d0b054a}'
PS C:\> $cmd = 'msiexec.exe /x {0} /qn TEST="my prop"' -f $id
PS C:\> Start-ProcessEx -CommandLine $cmd
```

Runs the process with the literal command line value `msiexec.exe /x {e97fa56f-daee-434f-ae00-5fab3d0b054a} /qn TEST="my prop"`.
If the argument `Test="my prop"` was used with `-ArgumentList` it would be escaped as the literal value `"Test=\"my prop\""`.
Using the `-CommandLine` parameter will disable any escaping rules and run the raw command exactly as it was passed in.

### Example 4: Start a process with redirected stdout to a file
```powershell
PS C:\> $fs = [IO.File]::Open('C:\stdout.txt', 'Create', 'Write', 'ReadWrite')
PS C:\> try
..   $si = New-StartupInfo -StandardOutput $fs.SafeFileHandle
..   Start-ProcessEx -FilePath cmd.exe /c echo hi -StartupInfo $si -Wait
.. }
.. finally {
..   $fs.Dispose()
.. }
```

Runs a process with the stdout redirected to the file at `C:\stdout.txt`.

## PARAMETERS

### -ApplicationName
Used with `-CommandLine` as the full path to the executable to run.
This is useful if the `-CommandLine` executable path contains spaces and you wish to be explicit about what to run.

```yaml
Type: String
Parameter Sets: CommandLine
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
Parameter Sets: FilePath
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
Parameter Sets: CommandLine
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CreationFlags
The process CreationFlags to set when starting the process.
Defaults to `None` if `-StartupInfo` contains a ConPTY otherwise the default is `NewConsole` to ensure the console application is created in a new console window instead of sharing the existing console.
You should not set `NewConsole` if a ConPTY is specified as that will have the new process ignore the ConPTY and use the new conhost allocated.
You cannot set `Suspended` with the `-Wait` parameter.

A suspended process can be started by calling `[ProcessEx.ProcessRunner]::ResumeThread($proc.Thread)`.

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

### -DisableInheritance
Explicitly disable all the handles in the current process from being inherited with the new process.
This cannot be used if `-StartupInfo` has an explicit StandardInput/Output/Error or InheritedHandles list.

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

### -Environment
A dictionary containing explicit environment variables to use for the new process.
These env vars will be used instead of the existing process environment variables if defined.
Use `Get-TokenEnvironment` to generate a new environment block that can be modified as needed for the new process.
Cannot be used with `-UseNewEnvironment`.

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
Parameter Sets: FilePath
Aliases:

Required: True
Position: 0
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

### -ProcessAttribute
Set the security descriptor and inheritbility of the new process handle.

```yaml
Type: SecurityAttributes
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

### -ThreadAttribute
Set the security descriptor and inheritbility of the new thread handle.

```yaml
Type: SecurityAttributes
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Token
Create the process to run with the access token specified.
This access token can be retrieved through other functions like `LogonUser`, duplicating from an existing process, etc.
Using this parameter calls `CreateProcessAsUser` instead of `CreateProcess` which requires the `SeIncreaseQuotaPrivilege` privilege.
It also requires the `SeAssignPrimaryTokenPrivilege` privilege if the token is not a restricted version of the callers primary token.
The `SeAssignPrimaryTokenPrivilege` privilege is not given to administrators by default so check with `whoami.exe /priv` to see if your account has this privilege.
The `Start-ProcessWith` cmdlet requires less sensitive privileges and can be used as an alternative to this cmdlet if needed.

Be aware that this process will add the Logon Session SID of this token to the station and desktop security descriptor specified by Desktop on `-StartupInfo`.
If no startup info was specified then the current station and desktop is used.
Running this with multiple tokens could hit the maximum allowed size in a ACL if the station/desktop descriptor is not cleaned up.

```yaml
Type: SafeHandle
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UseNewEnvironment
Instead of inheriting the current process environment variables, use a brand new set of environment variables for the current user.
Cannot be used with `-Environment`.


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

- `Process` - The `SafeHandle` of the process created

- `Thread` - The `SafeHandle` of the main thread

- `ProcessId` - Also aliased to `Id`, this is the process identifier

- `ThreadId` - The identifier of the main thread

- `ParentProcessId` - The process identifier of the parent that spawned this process

- `ExitCode` - The exit code of the process, this will not be set if it's still running

- `Environment` - The environment variables of the process

## NOTES

## RELATED LINKS

[CreateProcess](https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw)
[CreateProcessAsUser](https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessasuserw)
[Windows Privileges](https://docs.microsoft.com/en-us/windows/win32/secauthz/privilege-constants)
