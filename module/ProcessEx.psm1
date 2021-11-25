# Copyright: (c) 2021, Jordan Borean (@jborean93) <jborean93@gmail.com>
# MIT License (see LICENSE or https://opensource.org/licenses/MIT)

$importModule = Get-Command -Name Import-Module -Module Microsoft.PowerShell.Core
if (-not ('ProcessEx.Commands.NewStartupInfo' -as [type])) {
    $framework = if ($PSVersionTable.PSVersion.Major -eq 5) {
        'net472'
    }
    else {
        'netcoreapp3.1'
    }

    &$importModule ([IO.Path]::Combine($PSScriptRoot, 'bin', $framework, 'ProcessEx.dll')) -ErrorAction Stop
}
else {
    &$importModule -Force -Assembly ([ProcessEx.Commands.NewStartupInfo].Assembly)
}
