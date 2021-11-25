<#
.SYNOPSIS
Run Pester test

.PARAMETER PesterPath
The path to the Pester module to run.

.PARAMETER TestPath
The path to the tests to run

.PARAMETER OutputFile
The path to write the Pester test results to.
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [String]
    $PesterPath,

    [Parameter(Mandatory)]
    [String]
    $TestPath,

    [Parameter(Mandatory)]
    [String]
    $OutputFile
)

[PSCustomObject]$PSVersionTable |
    Select-Object -Property *, @{N='Architecture';E={
        switch ([IntPtr]::Size) {
            4 { 'x86' }
            8 { 'x64' }
            default { 'Unknown' }
        }
    }} |
    Format-List |
    Out-Host

Import-Module -Name $PesterPath -Force

$configuration = [PesterConfiguration]::Default
$configuration.Output.Verbosity = 'Detailed'
$configuration.Run.Exit = $true
$configuration.Run.Path = $TestPath
$configuration.TestResult.Enabled = $true
$configuration.TestResult.OutputPath = $OutputFile
$configuration.TestResult.OutputFormat = 'NUnitXml'

Invoke-Pester -Configuration $configuration -WarningAction Ignore
