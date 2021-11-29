[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration = 'Debug',

    [Parameter()]
    [string]
    $Task = 'Build',

    [Parameter()]
    [Switch]
    $Elevated
)

end {
    if ($PSEdition -eq 'Desktop') {
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor 'Tls12'
    }

    $modulePath = [IO.Path]::Combine($PSScriptRoot, 'tools', 'Modules')
    $requirements = Import-PowerShellDataFile ([IO.Path]::Combine($PSScriptRoot, 'requirements-dev.psd1'))
    foreach ($req in $requirements.GetEnumerator()) {
        $targetPath = [IO.Path]::Combine($modulePath, $req.Key)

        if (Test-Path -LiteralPath $targetPath) {
            Import-Module -Name $targetPath -Force -ErrorAction Stop
            continue
        }

        Write-Host "Installing build pre-req $($req.Key) as it is not installed"
        New-Item -Path $targetPath -ItemType Directory | Out-Null

        $webParams = @{
            Uri = "https://www.powershellgallery.com/api/v2/package/$($req.Key)/$($req.Value)"
            OutFile = [IO.Path]::Combine($modulePath, "$($req.Key).zip")  # WinPS requires the .zip extension to extract
            UseBasicParsing = $true
        }
        if ('Authentication' -in (Get-Command -Name Invoke-WebRequest).Parameters.Keys) {
            $webParams.Authentication = 'None'
        }

        $oldProgress = $ProgressPreference
        $ProgressPreference = 'SilentlyContinue'
        try {
            Invoke-WebRequest @webParams
            Expand-Archive -Path $webParams.OutFile -DestinationPath $targetPath -Force
            Remove-Item -LiteralPath $webParams.OutFile -Force
        }
        finally {
            $ProgressPreference = $oldProgress
        }

        Import-Module -Name $targetPath -Force -ErrorAction Stop
    }

    if ($Elevated) {
        # To avoid the parent runner locking these files on build, copy them to a temporary location.
        $moduleSource = [IO.Path]::Combine($PSScriptRoot, "output", "ProcessEx")
        $tempModulePath = [IO.Path]::Combine($PSScriptRoot, "tools", "Modules", "ProcessEx")
        if ((Test-Path -Path $moduleSource) -and (Test-Path -Path $tempModulePath)) {
            Remove-Item -Path $tempModulePath -Recurse -Force
        }

        if (-not (Test-Path $moduleSource)) {
            throw "Cannot elevate build runner without building project first"
        }
        Copy-Item -Path $moduleSource -Destination $tempModulePath -Recurse
        Import-Module -Name $tempModulePath

        $invokeSubProcessBuildSplat = @{
            Executable = (Get-Process -Id $pid).Path
            ArgumentList = "-File", $PSCommandPath, "-Configuration", $Configuration, "-Task", $Task
        }
        ./tools/ElevatePrivileges.ps1 @invokeSubProcessBuildSplat
    }
    else {
        $dotnetTools = @(dotnet tool list --global) -join "`n"
        if (-not $dotnetTools.Contains('coverlet.console')) {
            Write-Host 'Installing dotnet tool coverlet.console'
            dotnet tool install --global coverlet.console
        }

        $invokeBuildSplat = @{
            Task = $Task
            File = (Get-Item ([IO.Path]::Combine($PSScriptRoot, '*.build.ps1'))).FullName
            Configuration = $Configuration
        }
        Invoke-Build @invokeBuildSplat
    }
}
