---
external help file: ProcessEx.dll-Help.xml
Module Name: ProcessEx
online version: github.com/jborean93/ProcessEx/blob/main/docs/en-US/Get-ProcessEnvironment.md
schema: 2.0.0
---

# Get-ProcessEnvironment

## SYNOPSIS
Get the environment variables of a process.

## SYNTAX

```
Get-ProcessEnvironment [[-Process] <ProcessIntString[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Get the environment variables of a process in the form of a dictionary.

## EXAMPLES

### Example 1: Get the environment variables of the current process
```powershell
PS C:\> Get-ProcessEnvironment
```

Gets the environment variables of the currently running process.

### Example 2: Get the environment variables of a specific process
```powershell
PS C:\> Get-ProcessEnvironment -Id 1234
```

Gets the environment variables of the process `1234`.

## PARAMETERS

### -Process
The process to open.
This can either be a `Process` object or the process identifier.
If omited the current process will be used.

```yaml
Type: ProcessIntString[]
Parameter Sets: (All)
Aliases: Id

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### ProcessEx.ProcessIntString[]
The `Process` object, process id, or name.

## OUTPUTS

### System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
A dictionary containing the environment variables. Each key is case insensitive as environment variables on Windows are case insensitive.

## NOTES
This function uses undocumented APIs to retrieve the environment variables of other processes.
There are no guarantees that Windows will modify the APIs used and break this cmdlet in the future.

## RELATED LINKS
