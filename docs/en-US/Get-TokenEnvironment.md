---
external help file: ProcessEx.dll-Help.xml
Module Name: ProcessEx
online version: github.com/jborean93/ProcessEx/blob/main/docs/en-US/Get-TokenEnvironment.md
schema: 2.0.0
---

# Get-TokenEnvironment

## SYNOPSIS
Get environment block for a user token.

## SYNTAX

```
Get-TokenEnvironment [[-Token] <SafeHandle[]>] [<CommonParameters>]
```

## DESCRIPTION
Gets the environment variables for the user access token specified.
These environment variables can be used with `Start-ProcessEx` and `Start-ProcessWith` to specify custom environment variables for the user desired.

## EXAMPLES

### Example 1: Modify PATH envvar used for process with access token
```powershell
PS C:\> $env = Get-TokenEnvironment -Token $token
PS C:\> $env.PATH += "$([IO.Path]::PathSeparator)C:\folder\bin"
PS C:\> Start-ProcessEx -FilePath pwsh.exe -Token $token -Environment $env
```

Gets the environment block that would be used with a process spawned with a user access token.
The code adds a new entry to the `PATH` env var which is then applied to the new process that is spawned.

## PARAMETERS

### -Token
The access token that is used when building the environment variables.
If not specified the current process' access token is used to generate the environment values.
The token must have `Query` access and `Duplicate` access if it's a primary token.

```yaml
Type: SafeHandle[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Runtime.InteropServices.SafeHandle[]
The access tokens to build the environment variables from.

## OUTPUTS

### System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
A dictionary containing the environment variables. Each key is case insensitive as environment variables on Windows are case insensitive.

## NOTES

## RELATED LINKS

[CreateEnvironmentBlock](https://docs.microsoft.com/en-us/windows/win32/api/userenv/nf-userenv-createenvironmentblock)
