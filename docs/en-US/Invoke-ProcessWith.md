---
external help file: ProcessEx.dll-Help.xml
Module Name: ProcessEx
online version: github.com/jborean93/ProcessEx/blob/main/docs/en-US/Invoke-ProcessWith.md
schema: 2.0.0
---

# Invoke-ProcessWith

## SYNOPSIS
Invoke a process inline and capture the output like the call operator with the ability to run as another user or token.

## SYNTAX

### FilePath (Default)
```
Invoke-ProcessWith [-Credential <PSCredential>] [-Token <SafeHandle>] [-WithProfile] [-NetCredentialsOnly]
 [-FilePath] <String> [-ArgumentList <String[]>] [-ArgumentEscaping <ArgumentEscapingMode>]
 [-WorkingDirectory <String>] [-StartupInfo <StartupInfo>] [-Environment <IDictionary>] [-InputObject <Object>]
 [-InputEncoding <EncodingOrByteStream>] [-OutputEncoding <EncodingOrByteStream>]
 [-RedirectStdout <StdioStreamTarget>] [-RedirectStderr <StdioStreamTarget>] [-Raw]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### CommandLine
```
Invoke-ProcessWith [-Credential <PSCredential>] [-Token <SafeHandle>] [-WithProfile] [-NetCredentialsOnly]
 -CommandLine <String> [-ApplicationName <String>] [-WorkingDirectory <String>] [-StartupInfo <StartupInfo>]
 [-Environment <IDictionary>] [-InputObject <Object>] [-InputEncoding <EncodingOrByteStream>]
 [-OutputEncoding <EncodingOrByteStream>] [-RedirectStdout <StdioStreamTarget>]
 [-RedirectStderr <StdioStreamTarget>] [-Raw] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Invokes a process, waits for it to finish and returns the output back to PowerShell.
This is like the builtin call operator `& whoami` but with a few more features.

Some features it offers over the call operator are:

+ It can capture the raw bytes of the stdout/stderr streams
+ You can control the encoding used to pipe strings into and decode out of the process through a parameter rather than a global value
+ You can capture the raw bytes of the process rather than decode it to a string
+ The command can either be run from a single command line string or escaped through an array of arguments
+ You can run it as another user through the `-Token` or `-Credential` parameter

Some things that this cmdlet cannot do or is not designed for

+ Starting an interactive process - the output can be set to a console but it is not the default
+ Dealing with env var changes between `pwsh.exe` and `powershell.exe`, use an explicit `-Environment` block to handle this better
+ Using the PowerShell minishell `powershell { ... }` does not work

The cmdlet is also exposed through the `procwith` alias for easier interactive use.

Like the normal call operator, when a process is complete, the `$LASTEXITCODE` variable will be set to the exit code of the process.

By default the cmdlet is set to capture stdout lines in the output stream and stderr lines in the error stream.
If `$ErrorActionPreference = 'Stop'` or `-ErrorAction Stop` is set on the cmdlet, any stderr lines will terminate the statement.
Either explicitly set `-ErrorAction Continue` or use `-RedirectStderr` to something else to change this behaviour.

Unlike [Invoke-ProcessEx](./Invoke-ProcessEx.md), this cmdlet does not require special privileges that an Administrator user typically does not have.
While it can be run by less privileged users some of the things this API cannot do over `Invoke-ProcessEx` are:

+ The process is created in a new console window, it is not possible to redirect the stdout/stderr to the existing console directly
+ There is no way to run a process with a ConPTY, it must be redirected through the stdout/stderr pipes
+ Extended startup info elements like a parent process, inherited handles, job list is not supported
+ The Secondary Logon (`seclogon`) service must be running to call this API
+ The command line length is limited to a maximum of 1024 characters
+ It is not possible to disable inheritance of all inheritable handles in the current process

Use [Start-ProcessWith](./Start-ProcessWith.md) to invoke a process in the background.

See [Invoke-ProcessEx Examples](./Invoke-ProcessEx.md#examples) for some examples that also apply to `Invoke-ProcessWith`.

## EXAMPLES

### Example 1
```powershell
PS C:\> $cred = Get-Credential
PS C:\> $out = Invoke-ProcessWith whoami -Credential $cred
```

Invokes `whoami` but as the user supplied by the `-Credential` value.

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

### -ArgumentEscaping
The argument escaping mode to use when building the values of `-ArgumentList` to the single command line string.
The default `Standard` will escape the argument list according to the C style rules where whitespace is fully enclosed as a double quoted string.
The `Raw` rule will ignore all escaping and just appeach each argument with a space.
The `Msi` rule will quote the argument `FOO=value with space` as `FOO="value with space"`.

See [ConvertTo-EscapedArgument](./ConvertTo-EscapedArgument.md) for more information.

```yaml
Type: ArgumentEscapingMode
Parameter Sets: FilePath
Aliases:
Accepted values: Standard, Raw, Msi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ArgumentList
A list of arguments to run with the `-FilePath`.
These arguments are automatically escaped based on the Win32 C argument escaping rules as done by `ConvertTo-EscapedArgument`.
The `-ArgumentEscaping` parameter can be used to change the escaping method from the C style to other ones.
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

### -InputEncoding
Sets the encoding used when writing the input strings to the input of the process.
Will default to the value of `[Console]::InputEncoding` but can be set to an explicit `Encoding` type or one of the following string values:

+ `ASCII` - The 7-bit ASCII encoding
+ `ANSI` - The current system locale encoding
+ `BigEndianUnicode` - UTF-16 in Big Endian form
+ `BigEndianUtf32` - UTF-32 in Big Endian form
+ `ConsoleInput` - The console input encoding - `[Console]::InputEncoding` (Default)
+ `ConsoleOutput` - The console output encoding - `[Console]::OutputEncoding`
+ `OEM` - Same as `ConsoleOutput`
+ `Unicode` - UTF-16 in Little Endian form
+ `UTF8` - UTF-8 without a BOM
+ `UTF8Bom` - UTF-8 with a BOM
+ `UTF8NoBom` - UTF-8 without a BOM
+ `UTF32` - UTF-32 in Little Endiam form

Using any other string or integer value will call `[System.Text.Encoding]::GetEncoding($_)` to build the encoding object.

Setting `-InputEncoding Bytes` does not work as the input must have an encoding to convert the input strings to bytes when writing.

```yaml
Type: EncodingOrByteStream
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: ConsoleInput
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
The input objects to pipe into the process' stdin.
Any `byte` or `byte[]` array values will be passed in directly.
Any `string` or `string[]` values will be passed in as a line and encoded with the `-InputEncoding` parameter.
Other object types will be casted to a string and handled as above.

```yaml
Type: Object
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
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

### -OutputEncoding
Sets the encoding used when reading the output and error stream bytes.
Will default to the value of `[Console]::OutputEncoding` but can be set to an explicit `Encoding` type or one of the following string values:

+ `ASCII` - The 7-bit ASCII encoding
+ `ANSI` - The current system locale encoding
+ `BigEndianUnicode` - UTF-16 in Big Endian form
+ `BigEndianUtf32` - UTF-32 in Big Endian form
+ `Bytes` - Outputs as raw bytes and not a string
+ `ConsoleInput` - The console input encoding - `[Console]::InputEncoding`
+ `ConsoleOutput` - The console output encoding - `[Console]::OutputEncoding` (Default)
+ `OEM` - Same as `ConsoleOutput`
+ `Unicode` - UTF-16 in Little Endian form
+ `UTF8` - UTF-8 without a BOM
+ `UTF8Bom` - UTF-8 with a BOM
+ `UTF8NoBom` - UTF-8 without a BOM
+ `UTF32` - UTF-32 in Little Endiam form

Using any other string or integer value will call `[System.Text.Encoding]::GetEncoding($_)` to build the encoding object.

Setting `-OutputEncoding Bytes` will output the data as raw bytes rather than a string.

```yaml
Type: EncodingOrByteStream
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: ConsoleOutput
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

### -Raw
When set, will output as a single string rather than an array of strings representing each line.
If `-OutputEncoding Bytes`, the output is emitted as a single `byte[]` rather than as individual bytes.
This only applies to both the output (stdout) and error (stderr) stream .

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

### -RedirectStderr
Redirect the stderr data to another location.
Defaults to `Error` which is the PowerShell error stream.
Set to `Output` to redirect stderr to the output stream as a string based on `-OutputEncoding`.
Set to `Console` to skip capturing stderr together and write it directly to the console.
Set to `Null` to discard all output on stderr.

```yaml
Type: StdioStreamTarget
Parameter Sets: (All)
Aliases:
Accepted values: Error, Output, Console, Null

Required: False
Position: Named
Default value: Error
Accept pipeline input: False
Accept wildcard characters: False
```

### -RedirectStdout
Redirect the stdout data to another location.
Defaults to `Output` which is the PowerShell output stream.
Set to `Error` to redirect stdout to the error stream as an error record.
Set to `Console` to skip capturing stdout together and write it directly to the console.
Set to `Null` to discard all output on stdout.

```yaml
Type: StdioStreamTarget
Parameter Sets: (All)
Aliases:
Accepted values: Error, Output, Console, Null

Required: False
Position: Named
Default value: Output
Accept pipeline input: False
Accept wildcard characters: False
```

### -StartupInfo
Specify custom startup information as returned by `New-StartupInfo`.
It is not possible to use a custom `-StandardInput`, `-StandardOutput`, `-StandardError`, or `-ConPTY` in the startup info.
The `Invoke-ProcessWith` cmdlet also explicitly sets `ShowWindow` to `Hide` when running ignoring any existing value set there.

Certain startup info elemenents cannot be used by the `*With` process style cmdlets like `-ParentProcess`, `-InheritedHandle`, `-JobList`.

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

### System.String[]
The strings to write to the stdin of the new process. Each string will be appended with the value of `[Environment]::NewLine`. The encoding of each string is based on the value of `-InputEncoding`.

### System.Object
If a `byte` or `byte[]`, this input object will be written as is to the stdin of the new process. All other objects will be casted to a string and written as a line to stdin.

## OUTPUTS

### System.String
By default this cmdlet outputs the stdout lines as a string for each line. If `-OutputEncoding Bytes` is set, the output will instead be the bytes of stdout rather than a string. If `-Raw` is specified the output is a single string of the full stdout or a `byte[]` of the stdout if `-OutputEncoding Bytes` is set. There is no output if `-RedirectStdout` is set to `Console`, `Null`, or `Error`.

## NOTES
This cmdlet is meant to expand on the functionality of calling a specific process.

## RELATED LINKS
