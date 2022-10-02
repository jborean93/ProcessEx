---
external help file: ProcessEx.dll-Help.xml
Module Name: ProcessEx
online version: github.com/jborean93/ProcessEx/blob/main/docs/en-US/Get-ProcessEx.md
schema: 2.0.0
---

# Get-ProcessEx

## SYNOPSIS
Get info about the process.

## SYNTAX

```
Get-ProcessEx [-Process] <ProcessIntString[]> [[-Access] <ProcessAccessRights>] [-Inherit] [<CommonParameters>]
```

## DESCRIPTION
Get process information that isn't normally exposed in `Get-Process`.

## EXAMPLES

### Example 1: Get info about the current process
```powershell
PS C:\> Get-ProcessEx -Process $pid
```

Gets information about the current process

## PARAMETERS

### -Access
The desired access of the proces shandle the cmdlet will open.
Defaults to `AllAccess` but can be restricted depending on what the caller desires.

```yaml
Type: ProcessAccessRights
Parameter Sets: (All)
Aliases:
Accepted values: Terminate, CreateThread, VMOperation, VMRead, VMWrite, DupHandle, CreateProcess, SetQuota, SetInformation, QueryInformation, SuspendResume, QueryLimitedInformation, Delete, ReadControl, StandardRightsRead, StandardRightsExecute, StandardRightsWrite, WriteDAC, WriteOwner, StandardRightsRequired, Synchronize, StandardRightsAll, AllAccess, AccessSystemSecurity, GenericAll, GenericExecute, GenericWrite, GenericRead

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Inherit
Marks the opened process handle as inheritable to child processes it may spawn later on.

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

### -Process
The process to open.
This can either be a `Process` object or the process identifier.

```yaml
Type: ProcessIntString[]
Parameter Sets: (All)
Aliases: Id

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### ProcessEx.ProcessIntString[]
The `Process` object, process id, or name.

## OUTPUTS

### ProcessEx.ProcessInfo
The `ProcessInfo` of the opened process. This contains properties like the process handle and command line invocation. The `Thread` and `ThreadId` properties will not be set and should not be used with the output of this cmdlet. This object contains the following properties:

- `Executable` - The executable of the process

- `CommandLine` - The command line used to start the process

- `WorkingDirectory` - The working/current directory of the process

- `Process` - The `SafeHandle` of the process retrieved

- `Thread` - This is not set in the output from `Get-ProcessEx` and should be ignored

- `ProcessId` - Also aliased to `Id`, this is the process identifier

- `ThreadId` - This is not set in the output from `Get-ProcessEx` and should be ignored

- `ParentProcessId` - The process identifier of the parent that spawned this process

- `ExitCode` - The exit code of the process, this will not be set if it's still running

- `Environment` - The environment variables of the process

## NOTES

## RELATED LINKS
