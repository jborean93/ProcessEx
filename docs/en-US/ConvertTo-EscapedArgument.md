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
ConvertTo-EscapedArgument [-ArgumentEscaping <ArgumentEscapingMode>] [-InputObject] <String[]>
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Escapes the arguments supplied so they are seen as a distinct argument in the command line string.
The default escaping logic is based on the Windows C argument standard rules but some applications may not use this standard.
Using `-ArgumentEscaping Msi` the escaping logic can be set to escape MSI property keys and values alongside the C rules.
When escaping according to the `Msi` rules a `PROPERTY=value` is escaped as follows:

+ The `PROPERTY=` is kept as is
+ The `value` is quoted if it contains whitespace, double quotes, or is empty
+ The `value` will double up on any double quotes

If the argument does not match the MSI `PROPERTY=value` format, it is escaped according to the normal C rules.
An MSI property name must begin with either a litter or an underscore followed by letters, numerals, underscores, or periods.

## EXAMPLES

### Example 1: Escape value using C style rules
```powershell
PS C:\> $arguments = ConvertTo-EscapedArgument $argumentList
```

Escapes each argument in `$argumentList` for use with the raw CreateProcess command value.

### Example 2: Escape MSI style properties
```powershell
PS C:\> @(
    'FOO'
    'A=value'
    'B='
    'C=""'
    'D=value with space'
    'E=value with "quotes" and spaces'
    '1F=invalid prop wont be escaped on MSI rules'
    'C:\path with space'
) | ConvertTo-EscapedArgument -ArgumentEscaping Msi

# FOO
# A=value
# B=""
# C=""""""
# D="value with space"
# E="value with ""quotes"" and spaces"
# "1F=invalid prop wont be escaped on MSI rules"
# "C:\path with space"
```

Escaped each argument using the `Msi` ruleset.
Any argument that does not match the MSI `PROPERTY=value` format will be escaped based on the C style rules.
Some further notes

+ `B=` becomes `B=""` as a way to unset an MSI property
+ `C=""` becomes `C=""""""` as the `""` as both doubled up and the value is then quoted
+ `1F=invalid ...` is not treated as an MSI property because they cannot start with numbers

## PARAMETERS

### -ArgumentEscaping
The escaping rules to use.
The default `Standard` will use the C escaping rules.
The `Msi` ruleset will escape MSI property names and values, otherwise falls back to the standard C escaping rules.
The `Raw` will not escape any value and return it as is.

```yaml
Type: ArgumentEscapingMode
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Standard
Accept pipeline input: False
Accept wildcard characters: False
```

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
[MSI command line options](https://learn.microsoft.com/en-us/windows/win32/msi/command-line-options)
[MSI property name restrictions](https://learn.microsoft.com/en-us/windows/win32/msi/restrictions-on-property-names)
