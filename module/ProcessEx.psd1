# Copyright: (c) 2021, Jordan Borean (@jborean93) <jborean93@gmail.com>
# MIT License (see LICENSE or https://opensource.org/licenses/MIT)
#
# Module manifest for module 'ProcessEx'
#
# Generated by: Jordan Borean
#
# Generated on: 2021-11-22
#

@{

    # Script module or binary module file associated with this manifest.
    RootModule = if ($PSEdition -eq 'Core') {
        'bin/net6.0-windows/ProcessEx.dll'
    }
    else {
        'bin/net472/ProcessEx.dll'
    }

    # Version number of this module.
    ModuleVersion = '0.5.0'

    # Supported PSEditions
    # CompatiblePSEditions = @()

    # ID used to uniquely identify this module
    GUID = '95895cae-78d6-43c1-a87b-8450ee234693'

    # Author of this module
    Author = 'Jordan Borean'

    # Company or vendor of this module
    CompanyName = 'Community'

    # Copyright statement for this module
    Copyright = '(c) 2021 Jordan Borean. All rights reserved.'

    # Description of the functionality provided by this module
    Description = 'Exposes the Windows Process creation Win32 functions in PowerShell.`nSee https://github.com/jborean93/ProcessEx for more info'

    # Minimum version of the PowerShell engine required by this module
    PowerShellVersion = '5.1'

    # Minimum version of Microsoft .NET Framework required by this module. This prerequisite is valid for the PowerShell Desktop edition only.
    DotNetFrameworkVersion = '4.7.2'

    # Minimum version of the common language runtime (CLR) required by this module. This prerequisite is valid for the PowerShell Desktop edition only.
    ClrVersion = '4.0'

    # Processor architecture (None, X86, Amd64) required by this module
    # ProcessorArchitecture = ''

    # Modules that must be imported into the global environment prior to importing this module
    # RequiredModules = @()

    # Assemblies that must be loaded prior to importing this module
    # RequiredAssemblies = @()

    # Script files (.ps1) that are run in the caller's environment prior to importing this module.
    # ScriptsToProcess = @()

    # Type files (.ps1xml) to be loaded when importing this module
    TypesToProcess = @(
        'ProcessEx.Types.ps1xml'
    )

    # Format files (.ps1xml) to be loaded when importing this module
    # FormatsToProcess = @()

    # Modules to import as nested modules of the module specified in RootModule/ModuleToProcess
    NestedModules = @()

    # Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
    FunctionsToExport = @()

    # Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
    CmdletsToExport = @(
        'ConvertTo-EscapedArgument'
        'Copy-HandleToProcess'
        'Get-ProcessEnvironment'
        'Get-ProcessEx'
        'Get-StartupInfo'
        'Get-TokenEnvironment'
        'Invoke-ProcessEx'
        'Invoke-ProcessWith'
        'New-ConPTY'
        'New-StartupInfo'
        'Resize-ConPTY'
        'Start-ProcessEx'
        'Start-ProcessWith'
    )

    # Variables to export from this module
    VariablesToExport = @()

    # Aliases to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no aliases to export.
    AliasesToExport = @(
        'procex'
        'procwith'
    )

    # Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
    PrivateData = @{

        PSData = @{

            # Tags applied to this module. These help with module discovery in online galleries.
            Tags = @(
                'DevOps'
                'Process'
                'Windows'
                'Win32'
            )

            # A URL to the license for this module.
            LicenseUri = 'https://github.com/jborean93/ProcessEx/blob/main/LICENSE'

            # A URL to the main website for this project.
            ProjectUri = 'https://github.com/jborean93/ProcessEx'

            # A URL to an icon representing this module.
            # IconUri = ''

            # ReleaseNotes of this module
            ReleaseNotes = 'See https://github.com/jborean93/ProcessEx/blob/main/CHANGELOG.md'

            # Prerelease string of this module
            # Prerelease = ''

            # Flag to indicate whether the module requires explicit user acceptance for install/update/save
            # RequireLicenseAcceptance = $false

            # External dependent modules of this module
            # ExternalModuleDependencies = @()

        } # End of PSData hashtable

    } # End of PrivateData hashtable

}
