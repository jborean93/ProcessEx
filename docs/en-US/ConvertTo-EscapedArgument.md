---
external help file: ProcessEx.dll-Help.xml
Module Name: ProcessEx
online version: github.com/jborean93/ProcessEx/blob/main/docs/en-US/ConvertTo-EscapedArgument.md
schema: 2.0.0
---

# ConvertTo-EscapedArgument

## SYNOPSIS
Escape argument for the command line.

## SYNTAX

```
ConvertTo-EscapedArgument [-InputObject] <String[]> [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Escapes the arguments supplied so they are seen as a distinct argument in the command line string.
The escaping logic is based on the Windows C argument standard rules but some applications may not use this standard.

## EXAMPLES

### Example 1
```powershell
PS C:\> $arguments = ConvertTo-EscapedArgument $argumentList
```

Escapes each argument in `$argumentList` for use with the raw CreateProcess command value.

## PARAMETERS

### -InputObject
The arguments to escape.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
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

### System.String[]
The arguments to escape.

## OUTPUTS

### System.String
The escaped arguments.

## NOTES

## RELATED LINKS

[Parsing C Command-Line Arguments](https://docs.microsoft.com/en-us/cpp/c-language/parsing-c-command-line-arguments?view=msvc-170)
[Everyone quotes command line arguments the wrong way](https://docs.microsoft.com/en-us/archive/blogs/twistylittlepassagesallalike/everyone-quotes-command-line-arguments-the-wrong-way)
