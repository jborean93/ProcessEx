using System;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace ProcessEx
{
    internal static class ArgumentHelper
    {
        public static string ResolveExecutable(PSCmdlet cmdlet, string path)
        {
            return cmdlet.InvokeCommand.GetCommand(path, CommandTypes.Application).Source;
        }

        public static string EscapeArgument(string? argument)
        {
            if (String.IsNullOrEmpty(argument))
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
            return String.Format("\"{0}\"", newArg);
        }
    }
}
