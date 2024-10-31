using namespace System.IO
using namespace System.Runtime.InteropServices

#Requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration = 'Debug',

    [Parameter()]
    [ValidateSet('Build', 'Test')]
    [string]
    $Task = 'Build',

    [Parameter()]
    [Version]
    $PowerShellVersion = $PSVersionTable.PSVersion,

    [Parameter()]
    [Architecture]
    $PowerShellArch = [RuntimeInformation]::ProcessArchitecture,

    [Parameter()]
    [string]
    $ModuleNupkg,

    [Parameter()]
    [Switch]
    $Elevated
)

$ErrorActionPreference = 'Stop'

. ([Path]::Combine($PSScriptRoot, "tools", "common.ps1"))

$manifestPath = ([Path]::Combine($PSScriptRoot, 'manifest.psd1'))
$Manifest = [Manifest]::new($Configuration, $PowerShellVersion, $PowerShellArch, $manifestPath)

if ($ModuleNupkg) {
    Write-Host "Expanding module nupkg to '$($Manifest.ReleasePath)'" -ForegroundColor Cyan
    Expand-Nupkg -Path $ModuleNupkg -DestinationPath $Manifest.ReleasePath
}

Write-Host "Installing PowerShell dependencies" -ForegroundColor Cyan
$deps = $Task -eq 'Build' ? $Manifest.BuildRequirements : $Manifest.TestRequirements
$deps | Install-BuildDependencies

if ($Elevated) {
    # To avoid the parent runner locking these files on build, copy them to a temporary location.
    $moduleSource = [Path]::Combine($PSScriptRoot, "output", "ProcessEx")
    $tempModulePath = [Path]::Combine($PSScriptRoot, "output", "Modules", "ProcessEx")
    if ((Test-Path -LiteralPath $moduleSource) -and (Test-Path -LiteralPath $tempModulePath)) {
        Remove-Item -LiteralPath $tempModulePath -Recurse -Force
    }

    if (-not (Test-Path -Literal $moduleSource)) {
        throw "Cannot elevate build runner without building project first"
    }
    Copy-Item -LiteralPath $moduleSource -Destination $tempModulePath -Recurse
    Import-Module -Name $tempModulePath
    Import-Module -Name ([Path]::Combine($PSScriptRoot, "output", "Modules", "PSPrivilege"))

    $elevatedArguments = @(
        '-File', $PSCommandPath,
        '-Configuration', $Configuration
        '-Task', $Task
        '-PowerShellVersion', $PowerShellVersion
        '-PowerShellArch', $PowerShellArch
        if ($ModuleNupkg) {
            '-ModuleNupkg', $ModuleNupkg
        }
    )
    $invokeSubProcessBuildSplat = @{
        Executable = (Get-Process -Id $pid).Path
        ArgumentList = $elevatedArguments
    }
    ./tools/ElevatePrivileges.ps1 @invokeSubProcessBuildSplat
}
else {
    $buildScript = [Path]::Combine($PSScriptRoot, "tools", "InvokeBuild.ps1")
    $invokeBuildSplat = @{
        Task = $Task
        File = $buildScript
        Manifest = $manifest
    }
    Invoke-Build @invokeBuildSplat
}
