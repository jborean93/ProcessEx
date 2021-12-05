BeforeAll {
    . ([IO.Path]::Combine($PSScriptRoot, 'common.ps1'))
}

Describe "ConvertTo-EscapedArgument" {
    It "Escapes '<ArgumentList>' argument correctly" -TestCases @(
        @{ ArgumentList = @('a b c', 'd', 'e'); Expected = '"a b c" d e' }
        @{ ArgumentList = @('ab"c', '\', 'd'); Expected = '"ab\"c" \ d' }
        @{ ArgumentList = @('a\\\b', 'de fg', 'h'); Expected = 'a\\\b "de fg" h' }
        @{ ArgumentList = @('a\\b c', 'd', 'e'); Expected = '"a\\b c" d e' }
        @{ ArgumentList = @('CallMeIshmael'); Expected = 'CallMeIshmael' }
        @{ ArgumentList = @('Call Me Ishmael'); Expected = '"Call Me Ishmael"' }
        @{ ArgumentList = @('CallMe"Ishmael'); Expected = '"CallMe\"Ishmael"' }
        @{ ArgumentList = @('Call Me Ishmael\'); Expected = '"Call Me Ishmael\\"' }
        @{ ArgumentList = @('CallMe\"Ishmael'); Expected = '"CallMe\\\"Ishmael"' }
        @{ ArgumentList = @('a\\\b'); Expected = 'a\\\b' }
        @{ ArgumentList = @('C:\TEST A\'); Expected = '"C:\TEST A\\"' }
        @{ ArgumentList = @('"C:\TEST A\"'); Expected = '"\"C:\TEST A\\\""' }
        @{ ArgumentList = @('C:\Program Files\file\', 'arg with " quote'); Expected = '"C:\Program Files\file\\" "arg with \" quote"' }
        @{ ArgumentList = @($null); Expected = '""' }
        @{ ArgumentList = @(''); Expected = '""' }
        @{ ArgumentList = @('', $null, ''); Expected = '"" "" ""' }
    ) {
        param ($ArgumentList, $Expected)

        $actual = ($ArgumentList | ConvertTo-EscapedArgument) -join ' '
        $actual | Should -Be $Expected
    }
}
