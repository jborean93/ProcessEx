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

    It "Escapes <Argument> argument correctly using MSI" -TestCases @(
        @{ Argument = 'FOO'; Expected = 'FOO' }
        @{ Argument = 'A=value'; Expected = 'A=value' }
        @{ Argument = 'B='; Expected = 'B=""' }
        @{ Argument = 'C=""'; Expected = 'C=""""""' }
        @{ Argument = 'D=value with space'; Expected = 'D="value with space"' }
        @{ Argument = 'E=value with "quotes" and spaces'; Expected = 'E="value with ""quotes"" and spaces"' }
        @{ Argument = '1F=invalid prop wont be escaped on MSI rules'; Expected = '"1F=invalid prop wont be escaped on MSI rules"' }
        @{ Argument = 'C:\path with space'; Expected = '"C:\path with space"' }
        @{ Argument = 'F_.1=value with space'; Expected = 'F_.1="value with space"' }
        @{ Argument = '_F.1=value with space'; Expected = '_F.1="value with space"' }
    ) {
        param($Argument, $Expected)

        $actual = ConvertTo-EscapedArgument -ArgumentEscaping Msi -InputObject $Argument
        $actual | Should -Be $Expected
    }

    It "Escaped <Argument> argument correctly using Raw" -TestCases @(
        @{ Argument = 'A'; Expected = 'A' }
        @{ Argument = 'A B'; Expected = 'A B' }
        @{ Argument = '"A B"'; Expected = '"A B"' }
        @{ Argument = 'A \"Test\" B'; Expected = 'A \"Test\" B' }
    ) {
        param($Argument, $Expected)

        $actual = ConvertTo-EscapedArgument -ArgumentEscaping Raw -InputObject $Argument
        $actual | Should -Be $Expected
    }
}
