---
external help file: ProcessEx.dll-Help.xml
Module Name: ProcessEx
online version: github.com/jborean93/ProcessEx/blob/main/docs/en-US/New-StartupInfo.md
schema: 2.0.0
---

# New-StartupInfo

## SYNOPSIS
Create a StartupInfo object for creating a process.

## SYNTAX

### STDIO (Default)
```
New-StartupInfo [-Desktop <String>] [-Title <String>] [-Position <Coordinates>] [-WindowSize <Size>]
 [-CountChars <Size>] [-FillAttribute <ConsoleFill>] [-Flags <StartupInfoFlags>] [-WindowStyle <WindowStyle>]
 [-Reserved <String>] [-Reserved2 <Byte[]>] [-StandardInput <SafeHandle>] [-StandardOutput <SafeHandle>]
 [-StandardError <SafeHandle>] [-InheritedHandle <SafeHandle[]>] [-ParentProcess <Process>]
 [<CommonParameters>]
```

### ConPTY
```
New-StartupInfo [-Desktop <String>] [-Title <String>] [-Position <Coordinates>] [-WindowSize <Size>]
 [-CountChars <Size>] [-FillAttribute <ConsoleFill>] [-Flags <StartupInfoFlags>] [-WindowStyle <WindowStyle>]
 [-Reserved <String>] [-Reserved2 <Byte[]>] [-ConPTY <SafeHandle>] [-InheritedHandle <SafeHandle[]>]
 [-ParentProcess <Process>] [<CommonParameters>]
```

## DESCRIPTION
Create the StartupInfo object that defines the low level details used to create a process.

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

FIXME

## PARAMETERS

### -ConPTY
The pseudo console handle that represents the console input and output pipelines.
Use `New-ConsolePTY` to create this handle.
This cannot be set alongside the StandardInput/Output/Error parameters.
This will not work with `-ParentProcess` as there is no way to open the ConPTY in the parent process for inheritance.
This will not work with `Start-ProcessWith` due to the restrictions in the underlying APIs it calls.

```yaml
Type: SafeHandle
Parameter Sets: ConPTY
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CountChars
Size of the console screen buffer.

```yaml
Type: Size
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Desktop
The name of the Windows Desktop or both the Windows Desktop and Station (`station\desktop`) for the new process.
If not set then it defaults to the current station and desktop.

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

### -FillAttribute
Used to control the color of the background and foreground of the new console process.

```yaml
Type: ConsoleFill
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Flags
Flags to set for the new process.
Some flags are always set when other startup info parameters are specified.

```yaml
Type: StartupInfoFlags
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InheritedHandle
A list of handles to explicit allow the new process to inherit from.
If omitted then the child will inherit all the parent process' handles that were opened with inheritability if the process was created with inheritability.
When `-ParentProcess` is not set, all these handles will be changed to an inheritable handle in the current process.
When `-ParentProcess` is set then these handles need to be a valid handle in the parent process itself.
Use `Copy-HandleToProcess` to open them in the parent process.
This will not work with `Start-ProcessWith` due to the restrictions in the underlying APIs it calls.

```yaml
Type: SafeHandle[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ParentProcess
The parent process to create this as a child for.
If the parent process is running under a different user or a different elevation context of the current user then this will fail unless you are running as SYSTEM.
This will not work with `Start-ProcessWith` due to the restrictions in the underlying APIs it calls.

```yaml
Type: Process
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Position
The window position with X being the horizontal offset and Y being the vertical offset from the top left corner of the screen.

```yaml
Type: Coordinates
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Reserved
This parameter refers to the string `lpReserved` field in the `STARTUPINFO` structure used to create a process.
It is unused and undocumented in Windows.

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

### -Reserved2
This parameter refers to the byte `lpReserved2` field and `cbReserved2` in the `STARTUPINFO` structure.
This is unused and undocumented but has some used for sneaking data into a process.

```yaml
Type: Byte[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StandardError
A writable SafeHandle that is set to the new process' stderr pipe.
This handle will be explicitly set as inheritable process wide even if it wasn't opened as an inheritable object.
This cannot be used with `-DisableInheritance` on `Start-ProcessEx`.

```yaml
Type: SafeHandle
Parameter Sets: STDIO
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StandardInput
A readable SafeHandle that is set to the new process' stdin pipe.
This handle will be explicitly set as inheritable process wide even if it wasn't opened as an inheritable object.
This cannot be used with with `-DisableInheritance` on `Start-ProcessEx`.

```yaml
Type: SafeHandle
Parameter Sets: STDIO
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StandardOutput
A writable SafeHandle that is set to the new process' stdout pipe.
This handle will be explicitly set as inheritable process wide even if it wasn't opened as an inheritable object.
This cannot be used with `-DisableInheritance` on `Start-ProcessEx`.

```yaml
Type: SafeHandle
Parameter Sets: STDIO
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
This does not apply to GUI processes and console processes created in the same console.
For a new console process created with a new console window this is the title to display in the title bar and if not set the executable path is used.

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

### -WindowSize
The size of the window to create.

```yaml
Type: Size
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WindowStyle
The window style of the new process.

```yaml
Type: WindowStyle
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

### ProcessEx.StartupInfo
The startup info object that can be used when starting a new process.

## NOTES

## RELATED LINKS

[STARTUPINFOW](https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/ns-processthreadsapi-startupinfow)
[Undocumented STARTUPINFO Values](http://www.catch22.net/tuts/undocumented-createprocess#)
