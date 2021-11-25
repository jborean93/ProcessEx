---
external help file: ProcessEx.dll-Help.xml
Module Name: ProcessEx
online version: github.com/jborean93/ProcessEx/blob/main/docs/en-US/Get-StartupInfo.md
schema: 2.0.0
---

# Get-StartupInfo

## SYNOPSIS
Get the startupinfo for the current process.

## SYNTAX

```
Get-StartupInfo [<CommonParameters>]
```

## DESCRIPTION
Gets the `STARTUPINFO` values set for the current process.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-StartupInfo
```

Gets the `StartupInfo` value for the current process.
This can be used to inspect how the process was started by the caller.

## PARAMETERS

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### ProcessEx.StartupInfo
The `StartupInfo` value for the current process. Any of the extended thread attribute values, like `ParentProcess`, `ConPTY`, and `InheritedHandle` will not be set as they are used only for creating new processes.

## NOTES

## RELATED LINKS

[GetStartupInfo](https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getstartupinfow)
[STARTUPINFOW](https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/ns-processthreadsapi-startupinfow)
[Undocumented STARTUPINFO Values](http://www.catch22.net/tuts/undocumented-createprocess#)
