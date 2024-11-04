---
external help file: ProcessEx.dll-Help.xml
Module Name: ProcessEx
online version: github.com/jborean93/ProcessEx/blob/main/docs/en-US/Invoke-ProcessEx.md
schema: 2.0.0
---

# Invoke-ProcessEx

## SYNOPSIS
Invoke a process inline and capture the output like the call operator.

## SYNTAX

### FilePath (Default)
```
Invoke-ProcessEx [-DisableInheritance] [-Token <SafeHandle>] [-UseConPTY] [-ConPTYHeight <Int16>]
 [-ConPTYWidth <Int16>] [-UseNewEnvironment] [-FilePath] <String> [-ArgumentList <String[]>]
 [-ArgumentEscaping <ArgumentEscapingMode>] [-WorkingDirectory <String>] [-StartupInfo <StartupInfo>]
 [-Environment <IDictionary>] [-InputObject <Object>] [-InputEncoding <EncodingOrByteStream>]
 [-OutputEncoding <EncodingOrByteStream>] [-RedirectStdout <StdioStreamTarget>]
 [-RedirectStderr <StdioStreamTarget>] [-Raw] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### CommandLine
```
Invoke-ProcessEx [-DisableInheritance] [-Token <SafeHandle>] [-UseConPTY] [-ConPTYHeight <Int16>]
 [-ConPTYWidth <Int16>] [-UseNewEnvironment] -CommandLine <String> [-ApplicationName <String>]
 [-WorkingDirectory <String>] [-StartupInfo <StartupInfo>] [-Environment <IDictionary>] [-InputObject <Object>]
 [-InputEncoding <EncodingOrByteStream>] [-OutputEncoding <EncodingOrByteStream>]
 [-RedirectStdout <StdioStreamTarget>] [-RedirectStderr <StdioStreamTarget>] [-Raw]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Invokes a process, waits for it to finish and returns the output back to PowerShell.
This is like the builtin call operator `& whoami` but with a few more features.
Some features it offers over the call operator are:

+ It can capture the raw bytes of the stdout/stderr streams
+ You can control the encoding used to pipe strings into and decode out of the process through a parameter rather than a global value
+ You can capture the raw bytes of the process rather than decode it to a string
+ The command can either be run from a single command line string or escaped through an array of arguments
+ You can run it as another user through the `-Token` parameter
+ You can have access to extended startup info attributes like a parent process, ConPTY, explicit handle inheritance, etc

Some things that this cmdlet cannot do or is not designed for

+ Starting an interactive process - the output can be set to a console but it is not the default
+ Dealing with env var changes between `pwsh.exe` and `powershell.exe`, use `-UseNewEnvironment` or an explicit `-Environment` block to handle this better
+ Using the PowerShell minishell `powershell { ... }` does not work

The cmdlet is also exposed through the `procex` alias for easier interactive use.

It can be used to run a process as another user through the `-Token` parameter but requires the caller to have the `SeAssignPrimaryTokenPrivilege` and `SeIncreaseQuotaPrivilege` privileges which is not granted to an Administrator by default.
Like the normal call operator, when a process is complete, the `$LASTEXITCODE` variable will be set to the exit code of the process.

By default the cmdlet is set to capture stdout lines in the output stream and stderr lines in the error stream.
If `$ErrorActionPreference = 'Stop'` or `-ErrorAction Stop` is set on the cmdlet, any stderr lines will terminate the statement.
Either explicitly set `-ErrorAction Continue` or use `-RedirectStderr` to something else to change this behaviour.

Use [Invoke-ProcessWith](./Invoke-ProcessWith.md) which exposes a similar set of options but without the extra privilege requirement.

Use [Start-ProcessEx](./Start-ProcessEx.md) to invoke a process in the background.

## EXAMPLES

### Example 1: Invokes a process and captures the output
```powershell
PS C:\> $out = Invoke-ProcessEx pwsh.exe '-Command' '"test"'
```

Invokes `pwsh.exe -Command "test"` and captures the output into the `$out` var.
The encoding used defaults to the `[Console]::OutputEncoding` encoding but can be configured with `-OutputEncoding`.

### Example 2: Invokes a process and pipe data into it
```powershell
PS C:\> 'foo', 'bar' | Invoke-ProcessEx pwsh.exe '-Command' '$input'
```

Runs the binary `pwsh.exe` and pipes in the strings `foo` and `bar` as lines.
The command will just output it back but this shows how strings are piped into the new process.

### Example 3: Invoke binary data into a new process
```powershell
PS C:\> , (Get-Content -Path something.dll -AsByteStream -Raw) | Invoke-ProcessEx hexdump
```

Pipes in raw bytes to the `hexdump` executable.
These raw bytes are not subject to `-InputEncoding` and are written as is.
The command that wraps `, (Get-Content ...)` is not needed but makes writing the input bytes simpler and more efficient.
If running on PowerShell 5.1 change `-AsByteStream` to `-Encoding Bytes`.

### Example 4: Run binary with custom output encoding
```powershell
PS C:\> Invoke-ProcessEx winget list -OutputEncoding UTF8
```

Runs the command `winget list` and captures the raw output using the UTF-8 encoding rather than `[Console]::OutputEncoding`.
This is useful when interacting with commands that write with a codepage that does not use the console codepage.

### Example 5: Redirect stderr to the output stream
```powershell
# Writes hi to the output stream
PS C:\> Invoke-ProcessEx cmd.exe /c 'echo hi 1>&2' -RedirectStderr Output
# Also possible to achieve the same thing through pwsh redirection
# Note the stderr objects will be an ErrorRecord in this case
PS C:\> Invoke-ProcessEx cmd.exe /c 'echo hi 1>&2' 2>&1
# Writes hi as an ErrorRecord
PS C:\> Invoke-ProcessEx cmd.exe /c 'echo hi 1>&2'
```

The first command will redirect all stderr output to the output stream in PowerShell.
These objects are treated as strings like normal stdout.
The second command shows how stderr is normally written as an `ErrorRecord` in the error stream.
It may be necessary to right `-ErrorAction Continue` so that the error being written does not stop the pipeline if `$ErrorActionPreference = 'Stop'` is set.

### Example 6: Ignore stderr altogether
```powershell
PS C:\> Invoke-ProcessEx cmd.exe /c 'echo stdout && echo stderr 1>&2' -RedirectStderr Null
```

Ignores the all data written to the `stderr` stream and just capture the `stdout` data.
The same thing can be achieved with `Invoke-ProcessEx cmd.exe /c 'echo stdout && echo stderr 1>&2' 2>$null` but it is more efficient to use `-RedirectStderr Null` so PowerShell does not have to process it.

### Example 7: Capture the raw output as a byte array
```powershell
PS C:\> Invoke-ProcessEx python '-c' 'print("café")' -OutputEncoding Bytes | Format-Hex

#    Label: Byte (System.Byte) <3B8382A2>
#
#           Offset Bytes                                           Ascii
#                  00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F
#           ------ ----------------------------------------------- -----
# 0000000000000000 63 61 66 E9 0D 0A                               café��

# The above shows the encoding is not UTF-8 but the Windows locale encoding
# So we can run the below to capture it properly
PS C:\> Invoke-ProcessEx python '-c' 'print("café")' -OutputEncoding ANSI
```

Outputs the raw bytes of stdout and pipes them into `Format-Hex` to see the raw data a process will output.
This is useful for checking for encoding problems or just capturing raw bytes written by a process that isn't valid strings.
Adding `-Raw` will output the data as a single `byte[]` at the end rather than individual bytes.

### Example 8: Run a process with a ConPTY to get the exact console output
```powershell
PS C:\> Invoke-ProcessEx pwsh.exe '-Command' 'Get-Item $PSHome' -UseConPTY
```

Runs the process with a Console PTY that allows the caller to act as a console server.
This is used to capture and interact with a process as it would with a terminal.
The output will most likely contain ANSI escape codes and other interact terminal features that is hard to parse non-interactively.
Use `-UseConPTY` with caution as the process and the arguments given must explicitly close the process or else it will hang waiting for input that will never come.

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
Default value: Standard
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

### -ConPTYHeight
The ConPTY height to use when `-UseConPTY` is set.
Defaults to `$host.UI.RawUI.BufferSize.Height` or `80` if no host is present.

```yaml
Type: Int16
Parameter Sets: (All)
Aliases: ConPTYY

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ConPTYWidth
The ConPTY width to use when `-UseConPTY` is set.
Defaults to `$host.UI.RawUI.BufferSize.Width` or `80` if no host is present.

```yaml
Type: Int16
Parameter Sets: (All)
Aliases: ConPTYX

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
This parameter is ignored if `-UseConPTY` is specified as that always outputs as UTF-8.

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
Unless set to `Bytes`, this parameter is ignored if `-UseConPTY` is specified as that always outputs as UTF-8.

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

This option is ignored if `-UseConPTY` is set as there is no stderr anymore.

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

This option is ignored if `-UseConPTY` is set as there is no stdout anymore.

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

If using a custom parent process, the invoked process will be spawned in a new console and will not inherit the existing one.
Using `-RedirectStdout Console` or `-RedirectStderr Console` will not work if using a custom parent process.

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

### -UseConPTY
Use a Console PTY instead of the standard stdin, stdout, and stderr streams.
This replicates running a process in an actual console without any interaction and the output will reflect the data the executable will write to the console.
The data will typically contain interactive elements like ANSI escape codes.
The input and output encoding is always set to UTF-8 regardless of `-InputEncoding` and `-OutputEncoding` unless `-OutputEncoding Bytes` is specified.
In that case the output will be the raw bytes from the output handle.
When set `-RedirectStdout` and `-RedirectStderr` will do nothing.

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
