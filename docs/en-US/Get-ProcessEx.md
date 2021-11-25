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
This can either be a `Process` object, the process identifier, or the process executable.
The process executable as a string will only work if it finds only 1 process with that name on the host.

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
The `ProcessInfo` of the opened process. This contains properties like the process handle and command line invocation. The `Thread` and `ThreadId` properties will not be set and should not be used with the output of this cmdlet.

## NOTES

## RELATED LINKS
