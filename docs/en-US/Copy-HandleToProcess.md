---
external help file: ProcessEx.dll-Help.xml
Module Name: ProcessEx
online version: github.com/jborean93/ProcessEx/blob/main/docs/en-US/Copy-HandleToProcess.md
schema: 2.0.0
---

# Copy-HandleToProcess

## SYNOPSIS
Copy a handle to the target process.

## SYNTAX

```
Copy-HandleToProcess [-Process] <ProcessIntString> [[-Access] <Int32>] [-Inherit] [-Handle] <SafeHandle[]>
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Creates a copy of the handle in the process specified.
This can be used for `New-StartupInfo -InheritedHandle` when the `-ParentProcess` parameter is set.

## EXAMPLES

### Example 1: Duplicate handle for use with parent process inheritability
```powershell
PS C:\> $parentProcess = Get-Process -Id 6624
PS C:\> $fs = [IO.File]::Open('C:\file.txt', 'Create', 'Write', 'ReadWrite')
PS C:\> try {
>>   $newHandle = Copy-HandleToProcess -Handle $fs.SafeFileHandle -Process $parentProcess
>> }
>> finally {
>>   $fs.Dispose()
>> }
>> try {
>>   $si = New-StartupInfo -InheritedHandle $newHandle -ParentProcess $parentProcess
>>   Start-ProcessEx powershell.exe -StartupInfo $si -Wait
>> }
>> finally {
>>   $newHandle.Dispose()
>> }
```

Duplicates the file handle opened by the caller to the process specified.
This duplicated handle is marked to be inherited when creating the new process that has the same parent set as its parent.

## PARAMETERS

### -Access
The access requested for the new handle.
The value is dependent on the type of handle that is being copied.
If set to 0 or undefinined the duplicated handle will have the same access as the source handle.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Handle
The handle to copy to the other process.

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

### -Inherit
Marks the duplicated handle are inheritable in the target process.
This means the handle will inherit to and processes created by the target process if inheritbility is set.

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
The process to copy the handle to.
This can either be a `Process` object, the process identifier, or the process executable.
The process executable as a string will only work if it finds only 1 process with that name on the host.

```yaml
Type: ProcessIntString
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Runtime.InteropServices.SafeHandle[]
The handles to copy to the target process.

## OUTPUTS

### ProcessEx.Native.SafeDuplicateHandle
The duplicated handle that can be used in the target process. It is only valid in that target process but when disposed by the caller it will also close the handle in the target process. Use `$out.DangerousGetHandle()` to get a serialzied representation of the handle to pass to the target process.

## NOTES

When the output duplicate handle is disposed by the caller, it is also disposed in the target process.
Keep this handle alive until the target process has finished using it.

## RELATED LINKS

[DuplicateHandle](https://docs.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-duplicatehandle)
