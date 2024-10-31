@{
    InvokeBuildVersion = '5.11.3'
    PesterVersion = '5.6.1'
    BuildRequirements = @(
        @{
            ModuleName = 'Microsoft.PowerShell.PSResourceGet'
            ModuleVersion = '1.0.6'
        }
        @{
            ModuleName = 'OpenAuthenticode'
            RequiredVersion = '0.4.0'
        }
        @{
            ModuleName = 'platyPS'
            RequiredVersion = '0.14.2'
        }
        @{
            ModuleName = 'PSPrivilege'
            RequiredVersion = '0.2.0'
        }
    )
    TestRequirements = @(
        @{
            ModuleName = 'PSPrivilege'
            RequiredVersion = '0.2.0'
        }
    )
}
