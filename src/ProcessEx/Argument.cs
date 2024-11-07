using System;
using System.IO;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace ProcessEx;

public enum ArgumentEscapingMode
{
    Standard,
    Raw,
    Msi,
}

internal static class ArgumentHelper
{
    public static string ResolveExecutable(PSCmdlet cmdlet, string path, string workingDirectory)
    {
        if (path.StartsWith(".\\") || path.StartsWith("./") || path.StartsWith("..\\") || path.StartsWith("../"))
        {
            path = cmdlet.GetUnresolvedProviderPathFromPSPath(Path.Combine(workingDirectory, path));
        }

        return cmdlet.InvokeCommand.GetCommand(path, CommandTypes.Application)?.Source ?? path;
    }

    public static string EscapeArgument(string? argument)
    {
        if (string.IsNullOrEmpty(argument))
            return "\"\"";
        else if (!Regex.Match(argument, @"[\s""]", RegexOptions.None).Success)
            return argument ?? "";

        // Replace any double quotes in an argument with '\"'
        string newArg = Regex.Replace(argument, @"""", @"\""");

        // Double up on any '\' chars that preceded '\"'
        newArg = Regex.Replace(newArg, @"(\\+)\\""", @"$1$1\""");

        // Double up '\' at the end of the argument so it doesn't escape end quote.
        newArg = Regex.Replace(newArg, @"(\\+)$", "$1$1");

        // Finally wrap the entire argument in double quotes now we've escaped the double quotes within
        return string.Format("\"{0}\"", newArg);
    }

    public static string EscapeArgument(string? argument, ArgumentEscapingMode mode) => mode switch
    {
        ArgumentEscapingMode.Standard => EscapeArgument(argument),
        ArgumentEscapingMode.Raw => argument ?? "",
        ArgumentEscapingMode.Msi => EscapeMsiArgument(argument),
        _ => throw new ArgumentException("Unknown escaping mode specified"),
    };

    private static string EscapeMsiArgument(string? argument)
    {
        if (!Regex.Match(argument ?? "", @"^([a-zA-Z_][\w\.]*)=", RegexOptions.None).Success)
        {
            return EscapeArgument(argument);
        }

        int equalsIdx = argument!.IndexOf('=');
        string propName = argument.Substring(0, equalsIdx);
        string propValue = argument.Substring(equalsIdx + 1);

        // https://learn.microsoft.com/en-us/windows/win32/msi/command-line-options
        // Need to quote the value and escape " with "".
        bool quoteValue = propValue.Length == 0 || propValue.IndexOfAny([' ', '"']) != -1;
        propValue = propValue.Replace("\"", "\"\"");
        if (quoteValue)
        {
            propValue = $"\"{propValue}\"";
        }

        return $"{propName}={propValue}";
    }
}
