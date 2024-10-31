---
external help file: ProcessEx.dll-Help.xml
Module Name: ProcessEx
online version: github.com/jborean93/ProcessEx/blob/main/docs/en-US/New-ConPTY.md
schema: 2.0.0
---

# New-ConPTY

## SYNOPSIS
Create a new pseudo console object.

## SYNTAX

```
New-ConPTY [-Width] <Int16> [-Height] <Int16> [-InputPipe] <SafeHandle> [-OutputPipe] <SafeHandle>
 [-InheritCursor] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Create a new psuedo console object that can be used to read/write data to a child process.

## EXAMPLES

### Example 1: Create ConPTY backed by files
```powershell
PS C:\> $inputStream = [IO.FileStream]::Open('C:\temp\input', 'Open',
>>   'Read', 'ReadWrite')
PS C:\> $outputStream = [IO.FileStream]::Open('C:\temp\output', 'Create',
>>   'Write', 'ReadWrite')
PS C:\> $conPTYParams = @{
>>   Width = 80
>>   Height = 60
>>   InputPipe = $inputStream.SafeFileHandle
>>   OutputPipe = $outputStream.SafeFileHandle
>> }
PS C:\> $conPTY = New-ConPty @conPTYParams
PS C:\> $conPTY.Dispose()  # Should be disposed when finished.
```

Create a ConPTY that will pipe data from `C:\temp\input` into the ConPTY and write data from the ConPTY to `C:\temp\output`.

### Example 2: Create ConPTY backed by anonymous pipes
```powershell
PS C:\> $inputPipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("Out", "Inheritable")
PS C:\> $outputPipe = [System.IO.Pipes.AnonymousPipeServerStream]::new("In", "Inheritable")
PS C:\> $conPTYParams = @{
>>   Width = 80
>>   Height = 60
>>   InputPipe = $inputPipe.ClientSafePipeHandle
>>   OutputPipe = $outputPipe.ClientSafePipeHandle
>> }
PS C:\> $pty = New-ConPTY @conPTYParams
PS C:\> try {
>>   $inputPipe.ClientSafePipeHandle.Dispose()
>>   $outputPipe.ClientSafePipeHandle.Dispose()
>>   $si = New-StartupInfo -ConPTY $pty
>>   $proc = Start-ProcessEx powershell -StartupInfo $si -PassThru
>> }
>> finally {
>>   $pty.Dispose()
>> }
```

Create a ConPTY that will transmit data to and from an anonymous pipe created by the caller.
The caller can then read and write data on the server ends of the pipe to communicate with the child process.

## PARAMETERS

### -Height
The number of horizontal characters of the new buffer.

```yaml
Type: Int16
Parameter Sets: (All)
Aliases: Y

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -InheritCursor
The created ConPTY session will attempt to inherit the cursor position of the parent console.

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

### -InputPipe
A readable file or pipe handle that represents data to write as the input to the buffer.
The input bytes should be representable as a UTF-8 encoded string.

```yaml
Type: SafeHandle
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -OutputPipe
A writable file or pipe handle that represents data to read as the output of the buffer.
The bytes should be representable as a UTF-8 encoded string.

```yaml
Type: SafeHandle
Parameter Sets: (All)
Aliases:

Required: True
Position: 3
Default value: None
Accept pipeline input: True (ByPropertyName)
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

### -Width
The number of vertical characters of the new buffer.

```yaml
Type: Int16
Parameter Sets: (All)
Aliases: X

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Int16
The `Width` and `Height` as pipeline input by property name.

### System.Runtime.InteropServices.SafeHandle
The `InputPipe` and `OutputPipe` as pipeline input by property name.

## OUTPUTS

### ProcessEx.Native.SafeConsoleHandle
The `SafeConsoleHandle` representing the ConPTY. This should be disposed with `.Dispose()` when it is no longer needed to free up host resources.

## NOTES

The ConPTY is a new concept introduced in Windows 10 1809 build.
It is meant to provide an easy server side console API that applications can use to host console applications like a terminal.

## RELATED LINKS

[Introducing the Windows Pseudo Console (ConPTY)](https://devblogs.microsoft.com/commandline/windows-command-line-introducing-the-windows-pseudo-console-conpty/)
[CreatePseudoConsole](https://docs.microsoft.com/en-us/windows/console/createpseudoconsole)
