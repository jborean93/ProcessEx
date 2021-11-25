---
external help file: ProcessEx.dll-Help.xml
Module Name: ProcessEx
online version: github.com/jborean93/ProcessEx/blob/main/docs/en-US/Resize-ConPTY.md
schema: 2.0.0
---

# Resize-ConPTY

## SYNOPSIS
Resize a ConPTY buffer.

## SYNTAX

```
Resize-ConPTY [-ConPTY] <SafeHandle[]> [-Width] <Int16> [-Height] <Int16> [<CommonParameters>]
```

## DESCRIPTION
Resizes the width and height of a ConPTY buffer.

## EXAMPLES

### Example 1
```powershell
PS C:\> Resize-ConPTY -ConPTY $conPTY -Width 160 -Heigth 120
```

Resizes the buffer of the supplied ConPTY to have a width of 160 characters and a height of 120 characters.

## PARAMETERS

### -ConPTY
The ConPTY to resize as created by `New-ConPTY`.

```yaml
Type: SafeHandle[]
Parameter Sets: (All)
Aliases: InputObject

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### -Height
The new height of the ConPTY.

```yaml
Type: Int16
Parameter Sets: (All)
Aliases: Y

Required: True
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Width
The new width of the ConPTY

```yaml
Type: Int16
Parameter Sets: (All)
Aliases: X

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Runtime.InteropServices.SafeHandle[]
The ConPTY to resize.

## OUTPUTS

### None
## NOTES

## RELATED LINKS

[Introducing the Windows Pseudo Console (ConPTY)](https://devblogs.microsoft.com/commandline/windows-command-line-introducing-the-windows-pseudo-console-conpty/)
[ResizePseudocConsole](https://docs.microsoft.com/en-us/windows/console/resizepseudoconsole)
